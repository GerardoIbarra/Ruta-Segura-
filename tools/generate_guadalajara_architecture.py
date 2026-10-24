import bpy
import math
import os

OUTPUT_DIR = os.path.abspath(r"C:\Users\luisk\Ruta-Segura-\RutaSegura\Assets\_Project\Models\Architecture")
PROPS_DIR = os.path.abspath(r"C:\Users\luisk\Ruta-Segura-\RutaSegura\Assets\_Project\Models\Props")
os.makedirs(OUTPUT_DIR, exist_ok=True)
os.makedirs(PROPS_DIR, exist_ok=True)

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def get_or_create_material(name, color=(0.8, 0.8, 0.8, 1.0), roughness=0.5, metallic=0.0):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs['Base Color'].default_value = color
            bsdf.inputs['Roughness'].default_value = roughness
            if 'Metallic' in bsdf.inputs:
                bsdf.inputs['Metallic'].default_value = metallic
    return mat

def assign_material(obj, mat):
    if obj.data.materials:
        obj.data.materials[0] = mat
    else:
        obj.data.materials.append(mat)

def export_fbx(obj, filename, folder=OUTPUT_DIR):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    
    # Origen limpio en el piso (0, 0, 0)
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    
    filepath = os.path.join(folder, filename)
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        axis_forward='-Z',
        axis_up='Y',
        apply_scale_options='FBX_SCALE_ALL',
        object_types={'MESH'}
    )
    print(f"[EXPORT OK] {filename}")

# ==============================================================================
# MODELO 1: FACHADA CANTERA TRADICIONAL TAPATÍA (SM_Fachada_Cantera_A)
# Ancho X: 6m (-3 a +3) | Fondo Y: 2m (0 a +2 hacia atrás) | Alto Z: 8.5m (0 a 8.5)
# Frente hacia Y < 0 (hacia la calle en Blender) -> +Z en Unity (Frontal)
# ==============================================================================
def build_fachada_cantera():
    reset_scene()
    parts = []
    
    mat_cantera = get_or_create_material("M_Muro_Cantera", (0.82, 0.74, 0.60, 1.0), roughness=0.85)
    mat_cortina = get_or_create_material("M_Cortina_Metalica", (0.28, 0.30, 0.32, 1.0), roughness=0.4, metallic=0.7)
    mat_herreria = get_or_create_material("M_Herreria_Negra", (0.08, 0.08, 0.09, 1.0), roughness=0.5, metallic=0.8)
    mat_amarillo = get_or_create_material("M_Muro_AmarilloColonial", (0.85, 0.68, 0.28, 1.0), roughness=0.7)
    
    # 1. Muro Principal (X: 6m, Y fondo: 2.0m, Z alto: 8.5m)
    # Centro en X:0, Y:1.0 (de 0 a 2), Z:4.25 (de 0 a 8.5)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 1.0, 4.25))
    muro = bpy.context.active_object
    muro.scale = (6.0, 2.0, 8.5)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(muro, mat_amarillo)
    parts.append(muro)
    
    # 2. Zócalo de cantera inferior (planta baja frontal)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -0.05, 1.8))
    zocalo = bpy.context.active_object
    zocalo.scale = (6.05, 0.15, 3.6)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(zocalo, mat_cantera)
    parts.append(zocalo)
    
    # 3. Cortina metálica comercial cerrada
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -0.08, 1.65))
    cortina = bpy.context.active_object
    cortina.scale = (3.6, 0.08, 2.9)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(cortina, mat_cortina)
    parts.append(cortina)
    
    # 4. Marco de cantera alrededor de la cortina
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -0.12, 3.25))
    lintel = bpy.context.active_object
    lintel.scale = (4.2, 0.15, 0.35)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(lintel, mat_cantera)
    parts.append(lintel)
    
    for sign in [-1, 1]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(sign * 1.95, -0.12, 1.6))
        pilastra = bpy.context.active_object
        pilastra.scale = (0.35, 0.15, 3.2)
        bpy.ops.object.transform_apply(scale=True)
        assign_material(pilastra, mat_cantera)
        parts.append(pilastra)
        
    # 5. Ventanas y balcones de herrería planta alta
    for sign in [-1, 1]:
        wx = sign * 1.6
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(wx, -0.06, 5.8))
        v_frame = bpy.context.active_object
        v_frame.scale = (1.3, 0.12, 2.3)
        bpy.ops.object.transform_apply(scale=True)
        assign_material(v_frame, mat_cantera)
        parts.append(v_frame)
        
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(wx, -0.22, 5.2))
        barandal = bpy.context.active_object
        barandal.scale = (1.4, 0.28, 0.9)
        bpy.ops.object.transform_apply(scale=True)
        assign_material(barandal, mat_herreria)
        parts.append(barandal)

    # 6. Cornisa superior de cantera
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -0.10, 8.4))
    cornisa = bpy.context.active_object
    cornisa.scale = (6.2, 0.30, 0.45)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(cornisa, mat_cantera)
    parts.append(cornisa)

    bpy.ops.object.select_all(action='DESELECT')
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    edificio = bpy.context.active_object
    edificio.name = "SM_Fachada_Cantera_A"
    
    export_fbx(edificio, "SM_Fachada_Cantera_A.fbx")

# ==============================================================================
# MODELO 2: FACHADA COLONIAL TERRACOTA (SM_Fachada_Colonial_B)
# ==============================================================================
def build_fachada_colonial():
    reset_scene()
    parts = []
    
    mat_terracota = get_or_create_material("M_Muro_Terracota", (0.68, 0.32, 0.22, 1.0), roughness=0.8)
    mat_cantera = get_or_create_material("M_Muro_Cantera", (0.82, 0.74, 0.60, 1.0), roughness=0.85)
    mat_madera = get_or_create_material("M_Madera_Puerta", (0.24, 0.14, 0.08, 1.0), roughness=0.6)
    mat_herreria = get_or_create_material("M_Herreria_Negra", (0.08, 0.08, 0.09, 1.0), roughness=0.5, metallic=0.8)
    
    # 1. Muro Principal Terracota (6m x 2.0m x 7.8m)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 1.0, 3.9))
    muro = bpy.context.active_object
    muro.scale = (6.0, 2.0, 7.8)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(muro, mat_terracota)
    parts.append(muro)
    
    # 2. Zócalo cantera bajo
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -0.04, 0.45))
    zocalo = bpy.context.active_object
    zocalo.scale = (6.05, 0.12, 0.9)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(zocalo, mat_cantera)
    parts.append(zocalo)
    
    # 3. Portón colonial de madera gruesa
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(-1.4, -0.04, 1.6))
    puerta = bpy.context.active_object
    puerta.scale = (1.8, 0.1, 3.0)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(puerta, mat_madera)
    parts.append(puerta)
    
    # Marco de cantera
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(-1.4, -0.08, 1.65))
    m_puerta = bpy.context.active_object
    m_puerta.scale = (2.1, 0.16, 3.2)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(m_puerta, mat_cantera)
    parts.append(m_puerta)
    
    # 4. Ventana baja con reja de forja
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(1.5, -0.10, 1.6))
    reja = bpy.context.active_object
    reja.scale = (1.4, 0.22, 1.8)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(reja, mat_herreria)
    parts.append(reja)
    
    # 5. Balcón corrido en planta alta
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -0.30, 4.3))
    base_balcon = bpy.context.active_object
    base_balcon.scale = (5.2, 0.65, 0.25)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(base_balcon, mat_cantera)
    parts.append(base_balcon)
    
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -0.60, 4.85))
    baranda = bpy.context.active_object
    baranda.scale = (5.2, 0.1, 0.95)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(baranda, mat_herreria)
    parts.append(baranda)
    
    # 6. Cornisa superior
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -0.08, 7.7))
    cornisa = bpy.context.active_object
    cornisa.scale = (6.2, 0.25, 0.4)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(cornisa, mat_cantera)
    parts.append(cornisa)

    bpy.ops.object.select_all(action='DESELECT')
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    edificio = bpy.context.active_object
    edificio.name = "SM_Fachada_Colonial_B"
    
    export_fbx(edificio, "SM_Fachada_Colonial_B.fbx")

# ==============================================================================
# MODELO 3: FLORERÍA EL JAZMÍN - DESTINO (SM_Fachada_Floreria_Destino)
# ==============================================================================
def build_fachada_floreria():
    reset_scene()
    parts = []
    
    mat_verde = get_or_create_material("M_Floreria_Destino", (0.18, 0.58, 0.32, 1.0), roughness=0.5)
    mat_cantera = get_or_create_material("M_Muro_Cantera", (0.82, 0.74, 0.60, 1.0), roughness=0.85)
    mat_cristal = get_or_create_material("M_Cristal_Aparador", (0.15, 0.25, 0.30, 0.8), roughness=0.1, metallic=0.9)
    mat_madera = get_or_create_material("M_Madera_Puerta", (0.35, 0.20, 0.12, 1.0), roughness=0.5)
    mat_toldo = get_or_create_material("M_Toldo_Floreria", (0.85, 0.25, 0.25, 1.0), roughness=0.7)
    
    # 1. Muro Principal (6m x 2.0m x 7.5m)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 1.0, 3.75))
    muro = bpy.context.active_object
    muro.scale = (6.0, 2.0, 7.5)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(muro, mat_cantera)
    parts.append(muro)
    
    # 2. Gran Aparador de Cristal
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(1.0, -0.06, 1.6))
    cristal = bpy.context.active_object
    cristal.scale = (3.2, 0.08, 2.6)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(cristal, mat_cristal)
    parts.append(cristal)
    
    # 3. Puerta de Acceso
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(-1.8, -0.04, 1.5))
    puerta = bpy.context.active_object
    puerta.scale = (1.4, 0.1, 2.7)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(puerta, mat_madera)
    parts.append(puerta)
    
    # 4. Toldo Comercial Inclinado
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -0.65, 3.15))
    toldo = bpy.context.active_object
    toldo.scale = (5.6, 1.2, 0.08)
    toldo.rotation_euler = (math.radians(-18), 0, 0)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    assign_material(toldo, mat_toldo)
    parts.append(toldo)
    
    # 5. Letrero de Fachada
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, -0.15, 3.65))
    letrero = bpy.context.active_object
    letrero.scale = (5.0, 0.12, 0.65)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(letrero, mat_verde)
    parts.append(letrero)
    
    # 6. Maceteros exteriores
    for sign in [-1, 0, 1]:
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(sign * 1.1 + 0.8, -0.28, 0.3))
        maceta = bpy.context.active_object
        maceta.scale = (0.7, 0.40, 0.6)
        bpy.ops.object.transform_apply(scale=True)
        assign_material(maceta, mat_verde)
        parts.append(maceta)

    bpy.ops.object.select_all(action='DESELECT')
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    edificio = bpy.context.active_object
    edificio.name = "SM_Fachada_Floreria_Destino"
    
    export_fbx(edificio, "SM_Fachada_Floreria_Destino.fbx")

# ==============================================================================
# MODELO 4: FAROLA COLONIAL TAPATÍA (SM_Farola_Colonial_GDL)
# ==============================================================================
def build_farola():
    reset_scene()
    parts = []
    
    mat_herreria = get_or_create_material("M_Herreria_Negra", (0.08, 0.08, 0.09, 1.0), roughness=0.4, metallic=0.9)
    mat_foco = get_or_create_material("M_Foco_Luz", (1.0, 0.95, 0.8, 1.0), roughness=0.1)
    
    # Base octagonal
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.25, depth=0.7, location=(0, 0, 0.35))
    base = bpy.context.active_object
    assign_material(base, mat_herreria)
    parts.append(base)
    
    # Poste
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.08, depth=2.8, location=(0, 0, 2.1))
    poste = bpy.context.active_object
    assign_material(poste, mat_herreria)
    parts.append(poste)
    
    # Farol hexagonal
    bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=0.22, depth=0.45, location=(0.35, 0, 3.35))
    farol = bpy.context.active_object
    assign_material(farol, mat_foco)
    parts.append(farol)
    
    bpy.ops.mesh.primitive_cone_add(vertices=6, radius1=0.28, depth=0.20, location=(0.35, 0, 3.65))
    techo = bpy.context.active_object
    assign_material(techo, mat_herreria)
    parts.append(techo)

    bpy.ops.object.select_all(action='DESELECT')
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    farola = bpy.context.active_object
    farola.name = "SM_Farola_Colonial_GDL"
    
    export_fbx(farola, "SM_Farola_Colonial_GDL.fbx", folder=PROPS_DIR)

# ==============================================================================
# MODELO 5: BANQUETA TAPATÍA (SM_Banqueta_GDL)
# Ancho X: 6.0m | Profundidad Y: 2.5m (de 0 a +2.5 hacia atrás) | Alto Z: 0.18m
# ==============================================================================
def build_banqueta():
    reset_scene()
    parts = []
    
    mat_concreto = get_or_create_material("M_Concreto_Banqueta", (0.60, 0.60, 0.62, 1.0), roughness=0.8)
    
    # Acera / Banqueta (6.0m de largo en X, 2.5m de ancho en Y, 0.18m alto en Z)
    # Centro en X:0, Y:1.25, Z:0.09
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 1.25, 0.09))
    acera = bpy.context.active_object
    acera.scale = (6.0, 2.5, 0.18)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(acera, mat_concreto)
    parts.append(acera)
    
    # Guarnición / Machuelo en el borde frontal (Y = 0)
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0, 0.05, 0.09))
    machuelo = bpy.context.active_object
    machuelo.scale = (6.0, 0.12, 0.18)
    bpy.ops.object.transform_apply(scale=True)
    assign_material(machuelo, mat_concreto)
    parts.append(machuelo)

    bpy.ops.object.select_all(action='DESELECT')
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    banqueta = bpy.context.active_object
    banqueta.name = "SM_Banqueta_GDL"
    
    export_fbx(banqueta, "SM_Banqueta_GDL.fbx")

build_fachada_cantera()
build_fachada_colonial()
build_fachada_floreria()
build_farola()
build_banqueta()
print("[TODOS LOS MODELOS REGENERADOS CON COORDENADAS PERFECTAS]")
