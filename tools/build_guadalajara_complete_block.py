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

def box(parts, name, size, loc, material):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    o = bpy.context.active_object
    o.name = name
    o.scale = size
    bpy.ops.object.transform_apply(scale=True)
    if o.data.materials:
        o.data.materials[0] = material
    else:
        o.data.materials.append(material)
    parts.append(o)
    return o

def export_fbx(parts, filename, folder=OUTPUT_DIR):
    bpy.ops.object.select_all(action='DESELECT')
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    
    filepath = os.path.join(folder, filename)
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        axis_forward='Z',
        axis_up='Y',
        apply_scale_options='FBX_SCALE_ALL',
        object_types={'MESH'}
    )
    print(f"[OK] {filename}")

# ==============================================================================
# 1. MANZANA NORTE CONTINUA (Av. Juárez Norte)
# X: de -7.5 a -14.5 (7m de fondo macizo). Y: de -10 a +32 (42m largo continuo). Z: 8 a 11m alto
# Banqueta integrada: X de -5.0 a -7.5, Y de -10 a +32, Z: 0.18m alto
# ==============================================================================
def make_manzana_norte():
    reset()
    m = get_mats()
    p = []
    
    # Banqueta corrida con machuelo
    box(p, "Banqueta_Norte", (2.5, 42.0, 0.18), (-6.25, 11.0, 0.09), m['banqueta'])
    box(p, "Machuelo_Norte", (0.15, 42.0, 0.18), (-5.075, 11.0, 0.09), m['machuelo'])
    
    # 7 Edificios continuos sin huecos (cada uno de 6m de frente en Y)
    buildings = [
        (-7.0, 6.0, 8.5, m['amarillo'], 'colonial_porton'),
        (-1.0, 6.0, 8.0, m['terracota'], 'comercial_cortina'),
        (5.0, 6.0, 9.5, m['cantera'], 'neoclasico_balcon'),
        (11.0, 6.0, 8.2, m['blanco'], 'colonial_reja'),
        (17.0, 6.0, 8.8, m['terracota'], 'comercial_cortina'),
        (23.0, 6.0, 10.2, m['cantera'], 'neoclasico_balcon'),
        (29.0, 6.0, 8.5, m['amarillo'], 'colonial_porton')
    ]
    
    for by, bw, bh, bmat, bstyle in buildings:
        # Cuerpo macizo del edificio (7m de fondo hacia X negativo)
        box(p, f"Cuerpo_N_{by}", (7.0, bw, bh), (-11.0, by, bh / 2), bmat)
        
        # Zócalo de cantera en la base frontal
        box(p, f"Zocalo_N_{by}", (0.15, bw + 0.02, 0.5), (-7.575, by, 0.25), m['cantera'])
        
        # Cornisa superior moldurada
        box(p, f"Cornisa_N_{by}", (0.35, bw + 0.1, 0.45), (-7.675, by, bh - 0.22), m['cantera'])
        
        if bstyle == 'comercial_cortina':
            # Cortina metálica comercial cerrada
            box(p, f"Cortina_N_{by}", (0.08, bw * 0.65, 3.0), (-7.54, by, 1.8), m['cortina'])
            box(p, f"MarcoCortina_N_{by}", (0.18, bw * 0.75, 0.4), (-7.59, by, 3.5), m['cantera'])
            # Balcones superiores
            for sign in [-1, 1]:
                box(p, f"Balcon_N_{by}_{sign}", (0.45, 1.4, 0.9), (-7.725, by + sign * 1.5, 5.5), m['herreria'])
                box(p, f"MarcoV_N_{by}_{sign}", (0.12, 1.2, 2.2), (-7.56, by + sign * 1.5, 6.2), m['cantera'])
                
        elif bstyle == 'colonial_porton':
            # Portón colonial de madera
            box(p, f"Porton_N_{by}", (0.10, 2.0, 3.2), (-7.55, by - 1.2, 1.6), m['madera'])
            box(p, f"MarcoP_N_{by}", (0.18, 2.3, 3.4), (-7.59, by - 1.2, 1.7), m['cantera'])
            # Ventana con reja
            box(p, f"Reja_N_{by}", (0.25, 1.5, 1.8), (-7.625, by + 1.4, 1.6), m['herreria'])
            # Balcón corrido
            box(p, f"BalconC_N_{by}", (0.6, bw * 0.85, 0.95), (-7.8, by, 4.8), m['herreria'])
            box(p, f"BaseB_N_{by}", (0.6, bw * 0.85, 0.25), (-7.8, by, 4.2), m['cantera'])
            
        elif bstyle == 'neoclasico_balcon':
            # Pilastras verticales
            for py in [-2.5, 0, 2.5]:
                box(p, f"Pilastra_N_{by}_{py}", (0.2, 0.5, bh - 0.5), (-7.6, by + py, (bh - 0.5) / 2), m['cantera'])
            # Ventanales pisos 2 y 3
            for fz in [4.5, 7.5]:
                for sign in [-1, 1]:
                    box(p, f"Vent_N_{by}_{fz}_{sign}", (0.1, 1.2, 1.8), (-7.55, by + sign * 1.3, fz), m['cantera'])
                    box(p, f"Herr_N_{by}_{fz}_{sign}", (0.25, 1.3, 0.8), (-7.62, by + sign * 1.3, fz - 0.4), m['herreria'])
                    
        elif bstyle == 'colonial_reja':
            box(p, f"Puerta_N_{by}", (0.1, 1.6, 2.8), (-7.55, by - 1.5, 1.5), m['madera'])
            box(p, f"RejaBaja_N_{by}", (0.3, 2.4, 2.0), (-7.65, by + 1.0, 1.8), m['herreria'])
            box(p, f"BalconAlto_N_{by}", (0.5, 3.5, 0.9), (-7.75, by, 5.2), m['herreria'])

    export_fbx(p, "SM_Manzana_Juarez_Norte.fbx")

# ==============================================================================
# 2. MANZANA SUR CON ESQUINA A CALLE GALEANA
# Lado derecho de Av. Juárez (X: +7.5 a +14.5).
# Bloque 1: Y: -10 a +6 (16m largo).
# Bloque 2: Y: +16 a +32 (16m largo).
# Hueco de Calle Galeana: Y: +6 a +16 (10m de calle abierta a la derecha)
# ==============================================================================
def make_manzana_sur_esquina():
    reset()
    m = get_mats()
    p = []
    
    # 1. Bloque Sur Previo al Cruce (Y: -10 a +6)
    box(p, "Banqueta_Sur_1", (2.5, 16.0, 0.18), (6.25, -2.0, 0.09), m['banqueta'])
    box(p, "Machuelo_Sur_1", (0.15, 16.0, 0.18), (5.075, -2.0, 0.09), m['machuelo'])
    
    # Banqueta que dobla hacia Galeana (lado sur de Galeana, Y = 6.0)
    box(p, "Banqueta_Curva_Sur", (14.0, 2.5, 0.18), (14.5, 4.75, 0.09), m['banqueta'])
    box(p, "Machuelo_Curva_Sur", (14.0, 0.15, 0.18), (14.5, 5.925, 0.09), m['machuelo'])
    
    # Edificios Bloque 1
    b_sur_1 = [
        (-7.0, 6.0, 8.5, m['terracota']),
        (-1.0, 6.0, 9.2, m['cantera']),
        (4.0, 4.0, 8.0, m['amarillo']) # Edificio esquina sur con Galeana
    ]
    for by, bw, bh, bmat in b_sur_1:
        box(p, f"Cuerpo_S1_{by}", (7.0, bw, bh), (11.0, by, bh / 2), bmat)
        box(p, f"Zocalo_S1_{by}", (0.15, bw + 0.02, 0.5), (7.575, by, 0.25), m['cantera'])
        box(p, f"Cornisa_S1_{by}", (0.35, bw + 0.1, 0.45), (7.675, by, bh - 0.22), m['cantera'])
        box(p, f"Cortina_S1_{by}", (0.08, bw * 0.7, 3.0), (7.54, by, 1.8), m['cortina'])
        box(p, f"Balcon_S1_{by}", (0.4, bw * 0.7, 0.9), (7.7, by, 5.4), m['herreria'])
        
    # Fachada lateral del edificio de esquina sobre Galeana (X: 7.5 a 14.5, Y: 6.0)
    box(p, "Fachada_Lateral_Galeana_S", (7.0, 0.15, 8.0), (11.0, 6.075, 4.0), m['cantera'])
    box(p, "Ventana_Lateral_S", (2.0, 0.1, 2.2), (11.0, 6.05, 5.0), m['cantera'])
    box(p, "Reja_Lateral_S", (2.0, 0.25, 0.9), (11.0, 6.12, 4.5), m['herreria'])
    
    # 2. Bloque Sur Posterior al Cruce (Y: +16 a +32)
    box(p, "Banqueta_Sur_2", (2.5, 16.0, 0.18), (6.25, 24.0, 0.09), m['banqueta'])
    box(p, "Machuelo_Sur_2", (0.15, 16.0, 0.18), (5.075, 24.0, 0.09), m['machuelo'])
    
    # Banqueta norte de Galeana que dobla desde Juárez (Y = 16.0)
    box(p, "Banqueta_Curva_Norte", (14.0, 2.5, 0.18), (14.5, 17.25, 0.09), m['banqueta'])
    box(p, "Machuelo_Curva_Norte", (14.0, 0.15, 0.18), (14.5, 16.075, 0.09), m['machuelo'])
    
    b_sur_2 = [
        (18.0, 4.0, 8.0, m['amarillo']), # Esquina norte de Galeana
        (23.0, 6.0, 8.8, m['blanco']),
        (29.0, 6.0, 8.5, m['terracota'])
    ]
    for by, bw, bh, bmat in b_sur_2:
        box(p, f"Cuerpo_S2_{by}", (7.0, bw, bh), (11.0, by, bh / 2), bmat)
        box(p, f"Zocalo_S2_{by}", (0.15, bw + 0.02, 0.5), (7.575, by, 0.25), m['cantera'])
        box(p, f"Cornisa_S2_{by}", (0.35, bw + 0.1, 0.45), (7.675, by, bh - 0.22), m['cantera'])
        box(p, f"Cortina_S2_{by}", (0.08, bw * 0.7, 3.0), (7.54, by, 1.8), m['cortina'])
        box(p, f"Balcon_S2_{by}", (0.4, bw * 0.7, 0.9), (7.7, by, 5.4), m['herreria'])
        
    # Fachada lateral esquina norte sobre Galeana (X: 7.5 a 14.5, Y: 16.0)
    box(p, "Fachada_Lateral_Galeana_N", (7.0, 0.15, 8.0), (11.0, 15.925, 4.0), m['cantera'])

    export_fbx(p, "SM_Manzana_Juarez_Sur_Esquina.fbx")

# ==============================================================================
# 3. DESTINO: FLORERÍA EL JAZMÍN AL FONDO DE CALLE GALEANA
# Ubicada al final del corredor de Galeana en X = 20.0, de Y: 6.0 a Y: 16.0 (10m de frente)
# Mirando hacia X < 0 (hacia donde viene el jugador desde Juárez)
# ==============================================================================
def make_floreria_destino():
    reset()
    m = get_mats()
    p = []
    
    # Banqueta frontal de la florería (X: 18.0 a 20.0, Y: 6.0 a 16.0)
    box(p, "Banqueta_Floreria", (2.0, 10.0, 0.18), (19.0, 11.0, 0.09), m['banqueta'])
    box(p, "Machuelo_Floreria", (0.15, 10.0, 0.18), (18.075, 11.0, 0.09), m['machuelo'])
    
    # 1. Cuerpo macizo del edificio de la Florería (X: 20.0 a 27.0, Y: 6.0 a 16.0, Z: 8.5m)
    box(p, "Cuerpo_Floreria", (7.0, 10.0, 8.5), (23.5, 11.0, 4.25), m['blanco'])
    
    # 2. Zócalo de cantera en la base
    box(p, "Zocalo_Floreria", (0.15, 10.05, 0.5), (19.925, 11.0, 0.25), m['cantera'])
    
    # 3. Gran Escaparate / Aparador de Cristal para exhibir flores
    box(p, "Aparador_Floreria", (0.08, 5.5, 2.8), (19.94, 9.5, 1.7), m['cristal'])
    box(p, "Marco_Aparador", (0.15, 5.8, 3.0), (19.90, 9.5, 1.7), m['cantera'])
    
    # 4. Puerta de cristal y madera para acceder
    box(p, "Puerta_Floreria", (0.10, 1.8, 2.9), (19.92, 13.8, 1.55), m['madera'])
    box(p, "Marco_Puerta", (0.16, 2.1, 3.1), (19.88, 13.8, 1.65), m['cantera'])
    
    # 5. Toldo Comercial Bermellón / Rojo tradicional de florería tapatía
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(18.8, 11.0, 3.3))
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
    box(p, "Letrero_Floreria", (0.14, 8.5, 0.8), (19.88, 11.0, 4.0), m['floreria'])
    
    # 7. Macetones y estanterías exteriores con flores frente al aparador
    for dy in [-2.0, -0.6, 0.8, 2.2]:
        box(p, f"Maceton_{dy}", (0.7, 1.1, 0.6), (18.6, 11.0 + dy, 0.3), m['floreria'])
        box(p, f"Flores_{dy}", (0.6, 1.0, 0.4), (18.6, 11.0 + dy, 0.7), m['amarillo'])
        
    # 8. Planta alta con ventanales y balcones
    for dy in [-2.5, 2.5]:
        box(p, f"Balcon_F_{dy}", (0.45, 1.6, 0.9), (19.72, 11.0 + dy, 5.8), m['herreria'])
        box(p, f"Ventana_F_{dy}", (0.12, 1.5, 2.2), (19.90, 11.0 + dy, 6.4), m['cantera'])
        
    # 9. Cornisa superior de cantera
    box(p, "Cornisa_Floreria", (0.35, 10.2, 0.45), (19.78, 11.0, 8.3), m['cantera'])

    export_fbx(p, "SM_Manzana_Floreria_Destino.fbx")

# ==============================================================================
# 4. CALZADA DE ASFALTO CON SEÑALIZACIÓN (PASO DE CEBRA)
# ==============================================================================
def make_calzada():
    reset()
    m = get_mats()
    p = []
    
    # 1. Asfalto principal de Av. Juárez (X: -5.0 a +5.0 = 10m de ancho libre. Y: -10 a +32 = 42m de largo)
    box(p, "Asfalto_Juarez", (10.0, 42.0, 0.05), (0.0, 11.0, -0.025), m['asfalto'])
    
    # 2. Asfalto de Calle Galeana (X: +5.0 a +18.0 = 13m de largo. Y: +6.0 a +16.0 = 10m de ancho libre)
    box(p, "Asfalto_Galeana", (13.0, 10.0, 0.05), (11.5, 11.0, -0.025), m['asfalto'])
    
    # 3. Paso de cebra peatonal pintado en el asfalto (en el cruce de Galeana, Y = 6.0)
    for px in range(-4, 5, 2):
        box(p, f"Cebra_{px}", (1.0, 3.5, 0.005), (px, 7.0, 0.003), m['cebra'])

    export_fbx(p, "SM_Calzada_Asfalto_Completa.fbx")

# Ejecutar todo
make_manzana_norte()
make_manzana_sur_esquina()
make_floreria_destino()
make_calzada()
print("[TODOS LOS BLOQUES URBANOS MAESTROS EXPORTADOS EXITOSAMENTE]")
