import bpy
import math
import os

# ==========================================
# 1. GENERAR VALLA DE BLOQUEO CON LETREROS
# ==========================================
bpy.ops.wm.read_factory_settings(use_empty=True)

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

# Marco Tubular Exterior
tube_radius = 0.024
frame_w = 2.40
frame_h_top = 1.20
frame_h_bot = 0.20

# Tubo superior horizontal
add_cylinder("Tube_Top", tube_radius, frame_w, (0, 0, frame_h_top), (0, math.pi / 2, 0))

# Tubo inferior horizontal
add_cylinder("Tube_Bottom", tube_radius, frame_w, (0, 0, frame_h_bot), (0, math.pi / 2, 0))

# Tubos verticales laterales
vert_h = frame_h_top - frame_h_bot
add_cylinder("Tube_Left", tube_radius, vert_h, (-frame_w / 2, 0, (frame_h_top + frame_h_bot) / 2))
add_cylinder("Tube_Right", tube_radius, vert_h, (frame_w / 2, 0, (frame_h_top + frame_h_bot) / 2))

# Barrotes verticales interiores
bar_radius = 0.010
num_bars = 13
bar_h = frame_h_top - frame_h_bot - (tube_radius * 2)
bar_z = (frame_h_top + frame_h_bot) / 2
bar_spacing = (frame_w - 0.30) / (num_bars - 1)
start_x = - (frame_w - 0.30) / 2

for i in range(num_bars):
    bx = start_x + (i * bar_spacing)
    add_cylinder(f"Bar_{i}", bar_radius, bar_h, (bx, 0, bar_z))

# Patas de apoyo estables (apoyadas en Z = 0)
foot_radius = 0.020
foot_len = 0.60
foot_x_offset = 0.85

for side_x in [-foot_x_offset, foot_x_offset]:
    add_cylinder(f"Foot_Base_{side_x}", foot_radius, foot_len, (side_x, 0, foot_radius), (math.pi / 2, 0, 0))
    add_cylinder(f"Foot_Post_{side_x}", tube_radius, frame_h_bot, (side_x, 0, frame_h_bot / 2))
    # Tirantes diagonales
    brace_len = 0.30
    angle = math.radians(45)
    add_cylinder(f"Brace_F_{side_x}", 0.012, brace_len, (side_x, 0.12, 0.10), (angle, 0, 0))
    add_cylinder(f"Brace_B_{side_x}", 0.012, brace_len, (side_x, -0.12, 0.10), (-angle, 0, 0))

# Placas de Letrero (como en tu video: "CALLE CERRADA" y "DESVÍO")
plate_y = -0.025 # Ligeramente al frente de los barrotes
# Letrero Izquierdo (Calle Cerrada)
add_box("Placa_CalleCerrada", (0.65, 0.012, 0.32), (-0.50, plate_y, 0.72))
# Letrero Derecho (Desvío)
add_box("Placa_Desvio", (0.65, 0.012, 0.32), (0.50, plate_y, 0.72))

# Unir partes
# Crear un cubo temporal con rotación neutral como ancla activa
bpy.ops.mesh.primitive_cube_add(size=0.001, location=(0, 0, 0))
root_anchor = bpy.context.active_object
root_anchor.name = "SM_Valla_Bloqueo"

bpy.ops.object.select_all(action='DESELECT')
for p in mesh_parts:
    p.select_set(True)
root_anchor.select_set(True)
bpy.context.view_layer.objects.active = root_anchor

bpy.ops.object.join()
valla = bpy.context.active_object

# Materiales
mat_metal = bpy.data.materials.new(name="M_Metal_Valla")
mat_warning = bpy.data.materials.new(name="M_Valla_Bloqueo")

valla.data.materials.append(mat_metal)
valla.data.materials.append(mat_warning)

# Aplicar transformaciones limpias y pivote en la base Z=0
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
valla.location = (0, 0, 0)
bpy.context.scene.cursor.location = (0, 0, 0)
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')

unity_models_dir = os.path.abspath(r"C:\Users\luisk\Ruta-Segura-\RutaSegura\Assets\_Project\Models\Props")
blend_path = os.path.join(unity_models_dir, "Valla_Bloqueo.blend")
fbx_path = os.path.join(unity_models_dir, "SM_Valla_Bloqueo.fbx")

# Guardar .blend
bpy.ops.wm.save_as_mainfile(filepath=blend_path)

# Exportar FBX vertical para Unity (SIN bake_space_transform para evitar el doble giro)
bpy.ops.export_scene.fbx(
    filepath=fbx_path,
    use_selection=True,
    axis_forward='-Z',
    axis_up='Y',
    apply_scale_options='FBX_SCALE_ALL',
    object_types={'MESH'}
)
print("[OK] Valla de Bloqueo exportada verticalmente con éxito.")

# ==========================================
# 2. GENERAR CONO DE TRÁFICO (COMO EN EL VIDEO)
# ==========================================
bpy.ops.wm.read_factory_settings(use_empty=True)

# Base cuadrada del cono (0.38 x 0.38 x 0.03m)
bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0, 0.015))
base = bpy.context.active_object
base.scale = (0.38, 0.38, 0.03)
bpy.ops.object.transform_apply(scale=True)

# Cuerpo del cono (radio base 0.14, radio punta 0.03, altura 0.70m)
bpy.ops.mesh.primitive_cone_add(
    vertices=20,
    radius1=0.14,
    radius2=0.03,
    depth=0.70,
    location=(0, 0, 0.38)
)
body = bpy.context.active_object

# Unir base y cono
bpy.ops.object.select_all(action='DESELECT')
base.select_set(True)
body.select_set(True)
bpy.context.view_layer.objects.active = base
bpy.ops.object.join()
cono = bpy.context.active_object
cono.name = "SM_Cono_Trafico"

bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
cono.location = (0, 0, 0)
bpy.context.scene.cursor.location = (0, 0, 0)
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')

cono_fbx_path = os.path.join(unity_models_dir, "SM_Cono_Trafico.fbx")
bpy.ops.export_scene.fbx(
    filepath=cono_fbx_path,
    use_selection=True,
    axis_forward='-Z',
    axis_up='Y',
    apply_scale_options='FBX_SCALE_ALL',
    object_types={'MESH'}
)
print("[OK] Cono de Tráfico exportado con éxito.")
