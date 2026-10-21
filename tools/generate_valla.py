import bpy
import math
import os

# 1. Reiniciar escena limpia
bpy.ops.wm.read_factory_settings(use_empty=True)

# Lista para acumular todas las partes que se unirán
mesh_parts = []

def add_cylinder(name, radius, depth, location, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=16,
        radius=radius,
        depth=depth,
        location=location,
        rotation=rotation
    )
    obj = bpy.context.active_object
    obj.name = name
    mesh_parts.append(obj)
    return obj

def add_box(name, size, location):
    bpy.ops.mesh.primitive_cube_add(
        size=1.0,
        location=location
    )
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = size
    bpy.ops.object.transform_apply(scale=True)
    mesh_parts.append(obj)
    return obj

# 2. Marco Tubular Exterior (Radio: 2.2 cm = 0.022m)
tube_radius = 0.022
frame_w = 2.30
frame_h_top = 1.15
frame_h_bot = 0.18

# Tubo superior horizontal
add_cylinder("Tube_Top", tube_radius, frame_w, (0, 0, frame_h_top), (0, math.pi / 2, 0))

# Tubo inferior horizontal
add_cylinder("Tube_Bottom", tube_radius, frame_w, (0, 0, frame_h_bot), (0, math.pi / 2, 0))

# Tubo vertical izquierdo
vert_h = frame_h_top - frame_h_bot
add_cylinder("Tube_Left", tube_radius, vert_h, (-frame_w / 2, 0, (frame_h_top + frame_h_bot) / 2))

# Tubo vertical derecho
add_cylinder("Tube_Right", tube_radius, vert_h, (frame_w / 2, 0, (frame_h_top + frame_h_bot) / 2))

# 3. Barrotes verticales de contención (Radio: 0.01m)
bar_radius = 0.010
num_bars = 13
bar_h = frame_h_top - frame_h_bot - (tube_radius * 2)
bar_z = (frame_h_top + frame_h_bot) / 2
bar_spacing = (frame_w - 0.25) / (num_bars - 1)
start_x = - (frame_w - 0.25) / 2

for i in range(num_bars):
    bx = start_x + (i * bar_spacing)
    add_cylinder(f"Bar_{i}", bar_radius, bar_h, (bx, 0, bar_z))

# 4. Patas de apoyo estables (Pies tubulares con base en Z = 0)
foot_radius = 0.020
foot_len = 0.55
foot_x_offset = 0.85

for side_x in [-foot_x_offset, foot_x_offset]:
    # Barra en el suelo (a lo largo de Y)
    add_cylinder(f"Foot_Base_{side_x}", foot_radius, foot_len, (side_x, 0, foot_radius), (math.pi / 2, 0, 0))
    # Poste vertical que conecta el pie con el marco
    add_cylinder(f"Foot_Post_{side_x}", tube_radius, frame_h_bot, (side_x, 0, frame_h_bot / 2))
    # Tirantes diagonales de refuerzo
    brace_len = 0.28
    angle = math.radians(45)
    add_cylinder(f"Brace_F_{side_x}", 0.012, brace_len, (side_x, 0.12, 0.10), (angle, 0, 0))
    add_cylinder(f"Brace_B_{side_x}", 0.012, brace_len, (side_x, -0.12, 0.10), (-angle, 0, 0))

# 5. Placa central reflectante de advertencia
plate = add_box("Sign_Plate", (0.50, 0.008, 0.22), (0, 0, (frame_h_top + frame_h_bot) / 2))

# 6. Unir todas las piezas en un único objeto
bpy.ops.object.select_all(action='DESELECT')
for p in mesh_parts:
    p.select_set(True)

bpy.context.view_layer.objects.active = mesh_parts[0]
bpy.ops.object.join()
valla = bpy.context.active_object
valla.name = "SM_Valla_Bloqueo"

# 7. Configurar materiales
mat_metal = bpy.data.materials.new(name="M_Metal_Valla")
mat_metal.use_nodes = True
bsdf = mat_metal.node_tree.nodes.get("Principled BSDF")
if bsdf:
    # Metal galvanizado gris
    bsdf.inputs[0].default_value = (0.55, 0.57, 0.60, 1.0)
    # Metallic = 0.85, Roughness = 0.35
    if len(bsdf.inputs) > 1:
        bsdf.inputs["Metallic"].default_value = 0.85
        bsdf.inputs["Roughness"].default_value = 0.35

mat_warning = bpy.data.materials.new(name="M_Valla_Bloqueo")
mat_warning.use_nodes = True
bsdf_w = mat_warning.node_tree.nodes.get("Principled BSDF")
if bsdf_w:
    # Naranja reflectante de seguridad
    bsdf_w.inputs[0].default_value = (0.95, 0.45, 0.05, 1.0)

valla.data.materials.append(mat_metal)
valla.data.materials.append(mat_warning)

# 8. Asignar el pivote exacto en la base (Z=0, X=0, Y=0)
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
valla.location = (0, 0, 0)
bpy.context.scene.cursor.location = (0, 0, 0)
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')

# 9. Rutas de salida hacia Unity
unity_models_dir = os.path.abspath(r"C:\Users\luisk\Ruta-Segura-\RutaSegura\Assets\_Project\Models\Props")
blend_path = os.path.join(unity_models_dir, "Valla_Bloqueo.blend")
fbx_path = os.path.join(unity_models_dir, "SM_Valla_Bloqueo.fbx")

# Guardar .blend
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
print(f"[OK] Archivo Blender guardado en: {blend_path}")

# Exportar .fbx para Unity (ejes -Z Forward, Y Up, escala aplicada)
bpy.ops.export_scene.fbx(
    filepath=fbx_path,
    use_selection=True,
    axis_forward='-Z',
    axis_up='Y',
    apply_scale_options='FBX_SCALE_ALL',
    bake_space_transform=True,
    object_types={'MESH'}
)
print(f"[OK] Modelo FBX exportado para Unity en: {fbx_path}")
