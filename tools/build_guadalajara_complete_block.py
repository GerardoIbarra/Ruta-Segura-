import bpy
import math
import os

OUTPUT_DIR = os.path.abspath(r"C:\Users\luisk\Ruta-Segura-\RutaSegura\Assets\_Project\Models\Architecture")
PROPS_DIR = os.path.abspath(r"C:\Users\luisk\Ruta-Segura-\RutaSegura\Assets\_Project\Models\Props")
os.makedirs(OUTPUT_DIR, exist_ok=True)
os.makedirs(PROPS_DIR, exist_ok=True)

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def mat(name, color=(0.8, 0.8, 0.8, 1.0), roughness=0.6, metallic=0.0):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name=name)
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs['Base Color'].default_value = color
            bsdf.inputs['Roughness'].default_value = roughness
            if 'Metallic' in bsdf.inputs:
                bsdf.inputs['Metallic'].default_value = metallic
    return m

def get_mats():
    return {
        'cantera': mat("M_Muro_Cantera", (0.82, 0.74, 0.60, 1.0), roughness=0.85),
        'terracota': mat("M_Muro_Terracota", (0.68, 0.32, 0.22, 1.0), roughness=0.8),
        'amarillo': mat("M_Muro_AmarilloColonial", (0.85, 0.68, 0.28, 1.0), roughness=0.7),
        'blanco': mat("M_Muro_BlancoColonial", (0.88, 0.86, 0.82, 1.0), roughness=0.8),
        'cortina': mat("M_Cortina_Metalica", (0.28, 0.30, 0.33, 1.0), roughness=0.45, metallic=0.7),
        'herreria': mat("M_Herreria_Negra", (0.08, 0.08, 0.10, 1.0), roughness=0.55, metallic=0.8),
        'madera': mat("M_Madera_Puerta", (0.24, 0.14, 0.08, 1.0), roughness=0.5),
        'cristal': mat("M_Cristal_Aparador", (0.15, 0.25, 0.30, 0.8), roughness=0.1, metallic=0.9),
        'toldo': mat("M_Toldo_Floreria", (0.80, 0.22, 0.20, 1.0), roughness=0.6),
        'floreria': mat("M_Floreria_Destino", (0.18, 0.58, 0.32, 1.0), roughness=0.4),
        'banqueta': mat("M_Concreto_Banqueta", (0.60, 0.60, 0.62, 1.0), roughness=0.8),
        'machuelo': mat("M_Machuelo_Guarnicion", (0.75, 0.75, 0.72, 1.0), roughness=0.7),
        'asfalto': mat("M_Asfalto_Calle", (0.16, 0.16, 0.18, 1.0), roughness=0.85),
        'cebra': mat("M_Paso_Cebra_Blanco", (0.85, 0.85, 0.82, 1.0), roughness=0.6)
    }

# Funcion para colocar cajas usando coordenadas directas de Unity
# En Unity: X=ancho (izq/der), Y=altura (arriba), Z=longitud (adelante)
# Mapeo a Blender Z-Up:
#   Blender X = Unity X
#   Blender Y = -Unity Z  (para que +Z en Unity sea hacia adelante)
#   Blender Z = Unity Y   (para que +Y en Unity sea hacia arriba)
def add_unity_box(parts, name, size_xyz, pos_xyz, material):
    # size_xyz = (ancho_x, alto_y, largo_z)
    # pos_xyz  = (pos_x, pos_y, pos_z)
    blender_size = (size_xyz[0], size_xyz[2], size_xyz[1])
    blender_pos  = (pos_xyz[0], -pos_xyz[2], pos_xyz[1])
    
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=blender_pos)
    o = bpy.context.active_object
    o.name = name
    o.scale = blender_size
    bpy.ops.object.transform_apply(scale=True)
    if o.data.materials:
        o.data.materials[0] = material
    else:
        o.data.materials.append(material)
    parts.append(o)
    return o

def export_clean_fbx(parts, filename, folder=OUTPUT_DIR):
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    
    # Origen limpio en el piso (0, 0, 0) y rotación neutral 0
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    
    filepath = os.path.join(folder, filename)
    # Exportador estándar Blender a Unity FBX (Blender Z -> Unity Y, Blender -Y -> Unity Z)
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        axis_forward='-Z',
        axis_up='Y',
        apply_scale_options='FBX_SCALE_ALL',
        object_types={'MESH'}
    )
    print(f"[EXPORTADO 100% HORIZONTAL] {filename}")

# ==============================================================================
# 1. MANZANA NORTE CONTINUA (Av. Juárez Norte)
# X: de -7.5 a -14.5 (7m fondo macizo). Z: de -8 a +34 (42m largo continuo). Y: 8 a 11m alto
# Banqueta integrada: X de -5.0 a -7.5, Z de -8 a +34, Y: 0.18m alto
# ==============================================================================
def make_manzana_norte():
    reset()
    m = get_mats()
    p = []
    
    # Banqueta corrida con machuelo
    # size = (ancho_x, alto_y, largo_z)
    add_unity_box(p, "Banqueta_Norte", (2.5, 0.18, 42.0), (-6.25, 0.09, 13.0), m['banqueta'])
    add_unity_box(p, "Machuelo_Norte", (0.15, 0.18, 42.0), (-5.075, 0.09, 13.0), m['machuelo'])
    
    # 7 Edificios continuos sin huecos (cada uno de 6m de frente en Z)
    buildings = [
        (-5.0, 6.0, 8.5, m['amarillo'], 'colonial_porton'),
        (1.0, 6.0, 8.0, m['terracota'], 'comercial_cortina'),
        (7.0, 6.0, 9.5, m['cantera'], 'neoclasico_balcon'),
        (13.0, 6.0, 8.2, m['blanco'], 'colonial_reja'),
        (19.0, 6.0, 8.8, m['terracota'], 'comercial_cortina'),
        (25.0, 6.0, 10.2, m['cantera'], 'neoclasico_balcon'),
        (31.0, 6.0, 8.5, m['amarillo'], 'colonial_porton')
    ]
    
    for bz, bw, bh, bmat, bstyle in buildings:
        # Cuerpo macizo del edificio (7m de fondo hacia X negativo)
        add_unity_box(p, f"Cuerpo_N_{bz}", (7.0, bh, bw), (-11.0, bh / 2, bz), bmat)
        
        # Zócalo de cantera en la base frontal
        add_unity_box(p, f"Zocalo_N_{bz}", (0.15, 0.5, bw + 0.02), (-7.575, 0.25, bz), m['cantera'])
        
        # Cornisa superior moldurada
        add_unity_box(p, f"Cornisa_N_{bz}", (0.35, 0.45, bw + 0.1), (-7.675, bh - 0.22, bz), m['cantera'])
        
        if bstyle == 'comercial_cortina':
            add_unity_box(p, f"Cortina_N_{bz}", (0.08, 3.0, bw * 0.65), (-7.54, 1.8, bz), m['cortina'])
            add_unity_box(p, f"MarcoCortina_N_{bz}", (0.18, 0.4, bw * 0.75), (-7.59, 3.5, bz), m['cantera'])
            for sign in [-1, 1]:
                add_unity_box(p, f"Balcon_N_{bz}_{sign}", (0.45, 0.9, 1.4), (-7.725, 5.5, bz + sign * 1.5), m['herreria'])
                add_unity_box(p, f"MarcoV_N_{bz}_{sign}", (0.12, 2.2, 1.2), (-7.56, 6.2, bz + sign * 1.5), m['cantera'])
                
        elif bstyle == 'colonial_porton':
            add_unity_box(p, f"Porton_N_{bz}", (0.10, 3.2, 2.0), (-7.55, 1.6, bz - 1.2), m['madera'])
            add_unity_box(p, f"MarcoP_N_{bz}", (0.18, 3.4, 2.3), (-7.59, 1.7, bz - 1.2), m['cantera'])
            add_unity_box(p, f"Reja_N_{bz}", (0.25, 1.8, 1.5), (-7.625, 1.6, bz + 1.4), m['herreria'])
            add_unity_box(p, f"BalconC_N_{bz}", (0.6, 0.95, bw * 0.85), (-7.8, 4.8, bz), m['herreria'])
            add_unity_box(p, f"BaseB_N_{bz}", (0.6, 0.25, bw * 0.85), (-7.8, 4.2, bz), m['cantera'])
            
        elif bstyle == 'neoclasico_balcon':
            for pz in [-2.5, 0, 2.5]:
                add_unity_box(p, f"Pilastra_N_{bz}_{pz}", (0.2, bh - 0.5, 0.5), (-7.6, (bh - 0.5) / 2, bz + pz), m['cantera'])
            for fy in [4.5, 7.5]:
                for sign in [-1, 1]:
                    add_unity_box(p, f"Vent_N_{bz}_{fy}_{sign}", (0.1, 1.8, 1.2), (-7.55, fy, bz + sign * 1.3), m['cantera'])
                    add_unity_box(p, f"Herr_N_{bz}_{fy}_{sign}", (0.25, 0.8, 1.3), (-7.62, fy - 0.4, bz + sign * 1.3), m['herreria'])
                    
        elif bstyle == 'colonial_reja':
            add_unity_box(p, f"Puerta_N_{bz}", (0.1, 2.8, 1.6), (-7.55, 1.5, bz - 1.5), m['madera'])
            add_unity_box(p, f"RejaBaja_N_{bz}", (0.3, 2.0, 2.4), (-7.65, 1.8, bz + 1.0), m['herreria'])
            add_unity_box(p, f"BalconAlto_N_{bz}", (0.5, 0.9, 3.5), (-7.75, 5.2, bz), m['herreria'])

    export_clean_fbx(p, "SM_Manzana_Juarez_Norte.fbx")

# ==============================================================================
# 2. MANZANA SUR CON ESQUINA A CALLE GALEANA
# Lado derecho de Av. Juárez (X: +7.5 a +14.5).
# Bloque 1 previo al cruce: Z: -8 a +6 (14m largo).
# Bloque 2 posterior al cruce: Z: +16 a +34 (18m largo).
# Cruce de Calle Galeana hacia la derecha (+X): Z: +6 a +16 (10m de calle abierta)
# ==============================================================================
def make_manzana_sur_esquina():
    reset()
    m = get_mats()
    p = []
    
    # 1. Bloque Sur Previo al Cruce (Z: -8 a +6)
    add_unity_box(p, "Banqueta_Sur_1", (2.5, 0.18, 14.0), (6.25, 0.09, -1.0), m['banqueta'])
    add_unity_box(p, "Machuelo_Sur_1", (0.15, 0.18, 14.0), (5.075, 0.09, -1.0), m['machuelo'])
    
    # Banqueta que dobla hacia Galeana (lado sur de Galeana, Z = 6.0)
    add_unity_box(p, "Banqueta_Curva_Sur", (14.0, 0.18, 2.5), (14.5, 0.09, 4.75), m['banqueta'])
    add_unity_box(p, "Machuelo_Curva_Sur", (14.0, 0.18, 0.15), (14.5, 0.09, 5.925), m['machuelo'])
    
    b_sur_1 = [
        (-5.0, 6.0, 8.5, m['terracota']),
        (1.0, 6.0, 9.2, m['cantera']),
        (5.0, 2.0, 8.0, m['amarillo']) # Esquina sur con Galeana
    ]
    for bz, bw, bh, bmat in b_sur_1:
        add_unity_box(p, f"Cuerpo_S1_{bz}", (7.0, bh, bw), (11.0, bh / 2, bz), bmat)
        add_unity_box(p, f"Zocalo_S1_{bz}", (0.15, 0.5, bw + 0.02), (7.575, 0.25, bz), m['cantera'])
        add_unity_box(p, f"Cornisa_S1_{bz}", (0.35, 0.45, bw + 0.1), (7.675, bh - 0.22, bz), m['cantera'])
        add_unity_box(p, f"Cortina_S1_{bz}", (0.08, 3.0, bw * 0.7), (7.54, 1.8, bz), m['cortina'])
        add_unity_box(p, f"Balcon_S1_{bz}", (0.4, 0.9, bw * 0.7), (7.7, 5.4, bz), m['herreria'])
        
    # Fachada lateral del edificio de esquina sobre Galeana (X: 7.5 a 14.5, Z: 6.0)
    add_unity_box(p, "Fachada_Lateral_Galeana_S", (7.0, 8.0, 0.15), (11.0, 4.0, 6.075), m['cantera'])
    add_unity_box(p, "Ventana_Lateral_S", (2.0, 2.2, 0.1), (11.0, 5.0, 6.05), m['cantera'])
    add_unity_box(p, "Reja_Lateral_S", (2.0, 0.9, 0.25), (11.0, 4.5, 6.12), m['herreria'])
    
    # 2. Bloque Sur Posterior al Cruce (Z: +16 a +34)
    add_unity_box(p, "Banqueta_Sur_2", (2.5, 0.18, 18.0), (6.25, 0.09, 25.0), m['banqueta'])
    add_unity_box(p, "Machuelo_Sur_2", (0.15, 0.18, 18.0), (5.075, 0.09, 25.0), m['machuelo'])
    
    # Banqueta norte de Galeana que dobla desde Juárez (Z = 16.0)
    add_unity_box(p, "Banqueta_Curva_Norte", (14.0, 0.18, 2.5), (14.5, 0.09, 17.25), m['banqueta'])
    add_unity_box(p, "Machuelo_Curva_Norte", (14.0, 0.18, 0.15), (14.5, 0.09, 16.075), m['machuelo'])
    
    b_sur_2 = [
        (17.0, 2.0, 8.0, m['amarillo']), # Esquina norte de Galeana
        (22.0, 6.0, 8.8, m['blanco']),
        (28.0, 6.0, 8.5, m['terracota'])
    ]
    for bz, bw, bh, bmat in b_sur_2:
        add_unity_box(p, f"Cuerpo_S2_{bz}", (7.0, bh, bw), (11.0, bh / 2, bz), bmat)
        add_unity_box(p, f"Zocalo_S2_{bz}", (0.15, 0.5, bw + 0.02), (7.575, 0.25, bz), m['cantera'])
        add_unity_box(p, f"Cornisa_S2_{bz}", (0.35, 0.45, bw + 0.1), (7.675, bh - 0.22, bz), m['cantera'])
        add_unity_box(p, f"Cortina_S2_{bz}", (0.08, 3.0, bw * 0.7), (7.54, 1.8, bz), m['cortina'])
        add_unity_box(p, f"Balcon_S2_{bz}", (0.4, 0.9, bw * 0.7), (7.7, 5.4, bz), m['herreria'])
        
    # Fachada lateral esquina norte sobre Galeana (X: 7.5 a 14.5, Z: 16.0)
    add_unity_box(p, "Fachada_Lateral_Galeana_N", (7.0, 8.0, 0.15), (11.0, 4.0, 15.925), m['cantera'])

    export_clean_fbx(p, "SM_Manzana_Juarez_Sur_Esquina.fbx")

# ==============================================================================
# 3. DESTINO: FLORERÍA EL JAZMÍN AL FONDO DE CALLE GALEANA
# Ubicada al final del corredor de Galeana en X = 20.0, de Z: 6.0 a Z: 16.0 (10m de frente)
# ==============================================================================
def make_floreria_destino():
    reset()
    m = get_mats()
    p = []
    
    # Banqueta frontal de la florería (X: 18.0 a 20.0, Z: 6.0 a 16.0)
    add_unity_box(p, "Banqueta_Floreria", (2.0, 0.18, 10.0), (19.0, 0.09, 11.0), m['banqueta'])
    add_unity_box(p, "Machuelo_Floreria", (0.15, 0.18, 10.0), (18.075, 0.09, 11.0), m['machuelo'])
    
    # 1. Cuerpo macizo del edificio de la Florería (X: 20.0 a 27.0, Z: 6.0 a 16.0, Y: 8.5m)
    add_unity_box(p, "Cuerpo_Floreria", (7.0, 8.5, 10.0), (23.5, 4.25, 11.0), m['blanco'])
    
    # 2. Zócalo de cantera en la base
    add_unity_box(p, "Zocalo_Floreria", (0.15, 0.5, 10.05), (19.925, 0.25, 11.0), m['cantera'])
    
    # 3. Gran Escaparate / Aparador de Cristal para exhibir flores
    add_unity_box(p, "Aparador_Floreria", (0.08, 2.8, 5.5), (19.94, 1.7, 9.5), m['cristal'])
    add_unity_box(p, "Marco_Aparador", (0.15, 3.0, 5.8), (19.90, 1.7, 9.5), m['cantera'])
    
    # 4. Puerta de cristal y madera para acceder
    add_unity_box(p, "Puerta_Floreria", (0.10, 2.9, 1.8), (19.92, 1.55, 13.8), m['madera'])
    add_unity_box(p, "Marco_Puerta", (0.16, 3.1, 2.1), (19.88, 1.65, 13.8), m['cantera'])
    
    # 5. Toldo Comercial Bermellón inclinado
    # En Blender: pos_y = -pos_z_unity = -11.0, pos_z = pos_y_unity = 3.3
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(18.8, -11.0, 3.3))
    toldo = bpy.context.active_object
    toldo.name = "Toldo_Floreria"
    toldo.scale = (2.2, 9.5, 0.08)
    toldo.rotation_euler = (0, math.radians(-16), 0)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if toldo.data.materials:
        toldo.data.materials[0] = m['toldo']
    else:
        toldo.data.materials.append(m['toldo'])
    p.append(toldo)
    
    # 6. Letrero Comercial de Fachada "FLORERÍA EL JAZMÍN"
    add_unity_box(p, "Letrero_Floreria", (0.14, 0.8, 8.5), (19.88, 4.0, 11.0), m['floreria'])
    
    # 7. Macetones y estanterías exteriores con flores frente al aparador
    for dz in [-2.0, -0.6, 0.8, 2.2]:
        add_unity_box(p, f"Maceton_{dz}", (0.7, 0.6, 1.1), (18.6, 0.3, 11.0 + dz), m['floreria'])
        add_unity_box(p, f"Flores_{dz}", (0.6, 0.4, 1.0), (18.6, 0.7, 11.0 + dz), m['amarillo'])
        
    # 8. Planta alta con ventanales y balcones
    for dz in [-2.5, 2.5]:
        add_unity_box(p, f"Balcon_F_{dz}", (0.45, 0.9, 1.6), (19.72, 5.8, 11.0 + dz), m['herreria'])
        add_unity_box(p, f"Ventana_F_{dz}", (0.12, 2.2, 1.5), (19.90, 6.4, 11.0 + dz), m['cantera'])
        
    # 9. Cornisa superior de cantera
    add_unity_box(p, "Cornisa_Floreria", (0.35, 0.45, 10.2), (19.78, 8.3, 11.0), m['cantera'])

    export_clean_fbx(p, "SM_Manzana_Floreria_Destino.fbx")

# ==============================================================================
# 4. CALZADA DE ASFALTO CON SEÑALIZACIÓN (PASO DE CEBRA)
# ==============================================================================
def make_calzada():
    reset()
    m = get_mats()
    p = []
    
    # 1. Asfalto principal de Av. Juárez (X: -5.0 a +5.0 = 10m de ancho libre. Z: -8 a +34 = 42m de largo)
    add_unity_box(p, "Asfalto_Juarez", (10.0, 0.05, 42.0), (0.0, -0.025, 13.0), m['asfalto'])
    
    # 2. Asfalto de Calle Galeana (X: +5.0 a +18.0 = 13m de largo. Z: +6.0 a +16.0 = 10m de ancho libre)
    add_unity_box(p, "Asfalto_Galeana", (13.0, 0.05, 10.0), (11.5, -0.025, 11.0), m['asfalto'])
    
    # 3. Paso de cebra peatonal pintado en el asfalto (en el cruce de Galeana, Z = 7.0)
    for px in range(-4, 5, 2):
        add_unity_box(p, f"Cebra_{px}", (1.0, 0.005, 3.5), (px, 0.003, 7.0), m['cebra'])

    export_clean_fbx(p, "SM_Calzada_Asfalto_Completa.fbx")

# Ejecutar todo
make_manzana_norte()
make_manzana_sur_esquina()
make_floreria_destino()
make_calzada()
print("[TODOS LOS BLOQUES URBANOS MAESTROS REGENERADOS - 100% HORIZONTALES]")
