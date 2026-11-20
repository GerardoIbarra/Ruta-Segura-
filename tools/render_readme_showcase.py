# -*- coding: utf-8 -*-
"""
Renderiza 5 capturas de alta calidad (1280x720, Blender EEVEE) para la galeria del README:
  03_av_juarez_trafico_y_edificios.png
  04_muralla_barricada_y_manifestacion.png
  05_floreria_el_jazmin_y_calle_hospital.png
  06_catalogo_vehiculos_y_mobiliario.png
  07_catalogo_personajes_y_multitud.png
"""
import math
import os
import bpy
from mathutils import Vector

REPO = r"C:\Users\luisk\Ruta-Segura-"
MODELS_DIR = os.path.join(REPO, "RutaSegura", "Assets", "_Project", "Models")
ARCH_DIR = os.path.join(MODELS_DIR, "Architecture")
PROPS_DIR = os.path.join(MODELS_DIR, "Props")
TEX_DIR = os.path.join(REPO, "RutaSegura", "Assets", "_Project", "Textures")
OUT_DIR = os.path.join(REPO, "docs", "images")
os.makedirs(OUT_DIR, exist_ok=True)

TEX_CACHE = {}


def u2b(u_pos):
    return Vector((-u_pos[0], -u_pos[2], u_pos[1]))


def reset_scene(width=1280, height=720):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    TEX_CACHE.clear()
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x = width
    scene.render.resolution_y = height
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.view_settings.view_transform = 'AgX'
    scene.view_settings.look = 'AgX - Medium High Contrast'
    scene.view_settings.exposure = 0.45
    return scene


def load_image(rel_path):
    full = os.path.join(TEX_DIR, rel_path)
    if full in TEX_CACHE:
        return TEX_CACHE[full]
    if os.path.exists(full):
        img = bpy.data.images.load(full, check_existing=True)
        TEX_CACHE[full] = img
        return img
    return None


def set_bsdf_texture(mat, rel_path, roughness=None, emission_strength=0.0):
    img = load_image(rel_path)
    if img is None or not mat.use_nodes:
        return
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    if bsdf is None:
        return
    tex_node = nt.nodes.new("ShaderNodeTexImage")
    tex_node.image = img
    nt.links.new(tex_node.outputs["Color"], bsdf.inputs["Base Color"])
    if roughness is not None and "Roughness" in bsdf.inputs:
        bsdf.inputs["Roughness"].default_value = roughness
    if emission_strength > 0.0 and "Emission Color" in bsdf.inputs:
        nt.links.new(tex_node.outputs["Color"], bsdf.inputs["Emission Color"])
        bsdf.inputs["Emission Strength"].default_value = emission_strength


def set_bsdf_solid(mat, color, roughness=0.5, metallic=0.0, emission=None, emission_strength=0.0, coat=0.0):
    if not mat.use_nodes:
        return
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is None:
        return
    for link in list(mat.node_tree.links):
        if link.to_socket == bsdf.inputs["Base Color"]:
            mat.node_tree.links.remove(link)
    bsdf.inputs["Base Color"].default_value = (color[0], color[1], color[2], 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    if coat > 0.0 and "Coat Weight" in bsdf.inputs:
        bsdf.inputs["Coat Weight"].default_value = coat
    if emission is not None and "Emission Color" in bsdf.inputs:
        bsdf.inputs["Emission Color"].default_value = (emission[0], emission[1], emission[2], 1.0)
        bsdf.inputs["Emission Strength"].default_value = emission_strength


def enhance_materials():
    tex_map = {
        "M_Placa_CalleCerrada": ("Tex_CalleCerrada.png", 0.40, 0.0),
        "M_Placa_Desvio": ("Tex_Desvio.png", 0.40, 0.0),
        "M_Letrero_Floreria": ("Tex_Letrero_Floreria.png", 0.35, 0.45),
        "M_Placa_Calle_Juarez": ("Tex_Placa_Calle_Juarez.png", 0.45, 0.0),
        "M_Placa_Calle_Hospital": ("Tex_Placa_Calle_Hospital.png", 0.45, 0.0),
        "M_Manta_Protesta": ("Tex_Manta_Protesta.png", 0.70, 0.0),
        "M_Cartel_Neveria": ("Tex_Cartel_Neveria.png", 0.40, 0.20),
        "M_Cartel_Carmen": ("Tex_Cartel_Carmen.png", 0.45, 0.0),
        "M_Cartel_Periodico": ("Tex_Cartel_Periodico.png", 0.55, 0.0),
        "M_Grafiti_Consigna": ("Tex_Grafiti_Consigna.png", 0.80, 0.0),
        "M_Asfalto_Calle": (os.path.join("PBR", "T_Asfalto_Albedo.png"), 0.48, 0.0),
        "M_Concreto_Callejon": (os.path.join("PBR", "T_ConcretoCallejon_Albedo.png"), 0.65, 0.0),
        "M_Concreto_Banqueta": (os.path.join("PBR", "T_Banqueta_Albedo.png"), 0.72, 0.0),
        "M_Muro_Cantera": (os.path.join("PBR", "T_Cantera_Albedo.png"), 0.78, 0.0),
        "M_Muro_Ladrillo": (os.path.join("PBR", "T_Ladrillo_Albedo.png"), 0.80, 0.0),
        "M_Cortina_Metalica": (os.path.join("PBR", "T_CortinaMetalica_Albedo.png"), 0.42, 0.0),
    }
    glow_map = {
        "M_Foco_Luz": ((1.0, 0.88, 0.60), (1.0, 0.85, 0.52), 4.5, 0.2),
        "M_Faro_Blanco": ((1.0, 0.96, 0.86), (1.0, 0.95, 0.82), 5.0, 0.15),
        "M_Luz_Freno_Roja": ((0.95, 0.10, 0.10), (1.0, 0.12, 0.10), 4.5, 0.2),
        "M_Luz_Intermitente": ((1.0, 0.62, 0.06), (1.0, 0.58, 0.05), 4.5, 0.2),
        "M_Torreta_Roja": ((0.92, 0.10, 0.10), (1.0, 0.12, 0.10), 6.0, 0.15),
        "M_Torreta_Azul": ((0.12, 0.35, 1.0), (0.14, 0.42, 1.0), 6.0, 0.15),
        "M_Vidrio_Ventana": ((0.12, 0.18, 0.24), (0.95, 0.72, 0.42), 0.35, 0.12),
        "M_Cristal_Aparador": ((0.18, 0.28, 0.34), (0.90, 0.78, 0.52), 0.70, 0.10),
    }

    for mat in list(bpy.data.materials):
        base = mat.name.split('.')[0]
        if base in tex_map:
            rel, rough, em = tex_map[base]
            set_bsdf_texture(mat, rel, roughness=rough, emission_strength=em)
        elif base in glow_map:
            col, em_col, em_str, rough = glow_map[base]
            set_bsdf_solid(mat, col, roughness=rough, metallic=0.1, emission=em_col, emission_strength=em_str)


def make_mat(name, color, roughness=0.5, metallic=0.0, coat=0.0, emission=None, em_str=0.0):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name=name)
        mat.use_nodes = True
    set_bsdf_solid(mat, color, roughness=roughness, metallic=metallic, emission=emission, emission_strength=em_str, coat=coat)
    return mat


def import_fbx(folder, filename, unity_pos=(0.0, 0.0, 0.0), unity_yaw_deg=0.0, car_paint=None):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(folder, filename))
    imported = [o for o in bpy.data.objects if o not in before]
    roots = [o for o in imported if o.parent is None]

    holder = bpy.data.objects.new(f"Holder_{filename}", None)
    bpy.context.scene.collection.objects.link(holder)
    holder.location = u2b(unity_pos)
    holder.rotation_euler = (0.0, 0.0, -math.radians(unity_yaw_deg))

    for r in roots:
        r.parent = holder

    if car_paint is not None:
        paint_name, paint_rgb = car_paint
        pmat = make_mat(paint_name, paint_rgb, roughness=0.18, metallic=0.45, coat=1.0)
        for obj in imported:
            if obj.type == 'MESH' and obj.data:
                for idx, m in enumerate(obj.data.materials):
                    if m and "M_Auto_Carroceria" in m.name:
                        obj.data.materials[idx] = pmat
    return holder


def add_civilian(name, unity_pos, unity_yaw_deg, coat_rgb, placard=False, backpack=False, phone=False):
    holder = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(holder)
    holder.location = u2b(unity_pos)
    holder.rotation_euler = (0.0, 0.0, -math.radians(unity_yaw_deg))

    m_coat = make_mat(f"{name}_coat", coat_rgb, roughness=0.7)
    m_pants = make_mat(f"{name}_pants", (0.14, 0.17, 0.26), roughness=0.75)
    m_skin = make_mat(f"{name}_skin", (0.68, 0.49, 0.37), roughness=0.55)
    m_hair = make_mat(f"{name}_hair", (0.08, 0.07, 0.07), roughness=0.6)

    def part(pname, uloc, size, mat):
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.0, 0.0, 0.0))
        obj = bpy.context.active_object
        obj.name = pname
        obj.parent = holder
        obj.location = u2b(uloc)
        obj.scale = (size[0], size[2], size[1])
        obj.data.materials.append(mat)
        return obj

    part("Torso", (0, 1.15, 0), (0.42, 0.62, 0.24), m_coat)
    part("Head", (0, 1.60, 0), (0.22, 0.24, 0.22), m_skin)
    part("Hair", (0, 1.71, -0.01), (0.23, 0.10, 0.24), m_hair)
    part("LegL", (-0.10, 0.41, 0), (0.14, 0.82, 0.16), m_pants)
    part("LegR", (0.10, 0.41, 0), (0.14, 0.82, 0.16), m_pants)
    part("ArmL", (-0.28, 1.14, 0), (0.12, 0.54, 0.14), m_coat)
    part("ArmR", (0.28, 1.14, 0), (0.12, 0.54, 0.14), m_coat)

    if backpack:
        part("Pack", (0, 1.18, -0.17), (0.30, 0.40, 0.14), m_pants)
    if placard:
        m_sign = make_mat(f"{name}_sign", (0.92, 0.89, 0.80), roughness=0.5)
        part("Stick", (0.30, 1.75, 0.18), (0.04, 1.10, 0.04), m_hair)
        part("Board", (0.30, 2.25, 0.20), (0.68, 0.46, 0.03), m_sign)
    if phone:
        m_glow = make_mat(f"{name}_phone", (0.25, 0.82, 1.0), roughness=0.2, emission=(0.25, 0.82, 1.0), em_str=3.5)
        part("Phone", (0.24, 1.24, 0.24), (0.09, 0.15, 0.02), m_glow)
    return holder


def setup_sunset_world_and_lights(sun_euler=(68, 0, -145), sun_energy=4.2):
    world = bpy.data.worlds.new("GDL_SunsetWorld")
    bpy.context.scene.world = world
    world.use_nodes = True
    nt = world.node_tree
    bg = nt.nodes.get("Background")
    if bg:
        tex_coord = nt.nodes.new("ShaderNodeTexCoord")
        sep = nt.nodes.new("ShaderNodeSeparateXYZ")
        ramp = nt.nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.elements[0].position = 0.0
        ramp.color_ramp.elements[0].color = (0.78, 0.42, 0.24, 1.0)
        e_mid = ramp.color_ramp.elements.new(0.22)
        e_mid.color = (0.26, 0.34, 0.50, 1.0)
        ramp.color_ramp.elements[1].position = 0.72
        ramp.color_ramp.elements[1].color = (0.10, 0.15, 0.30, 1.0)
        nt.links.new(tex_coord.outputs["Generated"], sep.inputs["Vector"])
        nt.links.new(sep.outputs["Z"], ramp.inputs["Fac"])
        nt.links.new(ramp.outputs["Color"], bg.inputs["Color"])
        bg.inputs["Strength"].default_value = 1.65

    sun_data = bpy.data.lights.new(name="Sun_Atardecer", type='SUN')
    sun_data.energy = sun_energy
    sun_data.color = (1.0, 0.80, 0.58)
    sun_obj = bpy.data.objects.new(name="Sun_Atardecer", object_data=sun_data)
    bpy.context.scene.collection.objects.link(sun_obj)
    sun_obj.rotation_euler = tuple(math.radians(a) for a in sun_euler)


def add_point_light(name, unity_pos, color, energy=180.0, radius=0.4):
    ld = bpy.data.lights.new(name=name, type='POINT')
    ld.color = color
    ld.energy = energy
    ld.shadow_soft_size = radius
    lo = bpy.data.objects.new(name=name, object_data=ld)
    bpy.context.scene.collection.objects.link(lo)
    lo.location = u2b(unity_pos)
    return lo


def render_view(out_name, cam_unity_pos, target_unity_pos, lens=28.0):
    cam_data = bpy.data.cameras.new(name="RenderCam")
    cam_data.lens = lens
    cam_data.clip_start = 0.1
    cam_data.clip_end = 500.0
    cam_obj = bpy.data.objects.new(name="RenderCam", object_data=cam_data)
    bpy.context.scene.collection.objects.link(cam_obj)

    p_cam = u2b(cam_unity_pos)
    p_tgt = u2b(target_unity_pos)
    cam_obj.location = p_cam
    direction = p_tgt - p_cam
    cam_obj.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()

    bpy.context.scene.camera = cam_obj
    out_path = os.path.join(OUT_DIR, out_name)
    bpy.context.scene.render.filepath = out_path
    bpy.ops.render.render(write_still=True)
    print(f"[RENDERED] {out_path}")


def build_av_juarez_base():
    import_fbx(ARCH_DIR, "SM_Ruta_Piso_Completo.fbx")
    import_fbx(ARCH_DIR, "SM_Manzanas_Juarez_Hidalgo.fbx")
    import_fbx(ARCH_DIR, "SM_Manzanas_Callejones_Galeana.fbx")

    # Farolas
    for z in [20, 34, 48, 62]:
        import_fbx(PROPS_DIR, "SM_Farola_Colonial_GDL.fbx", (-6.8, 0.18, z), 0)
        import_fbx(PROPS_DIR, "SM_Farola_Colonial_GDL.fbx", (6.8, 0.18, z + 7), 180)
        add_point_light(f"LampW_{z}", (-5.8, 3.6, z), (1.0, 0.78, 0.48), 220.0)
        add_point_light(f"LampE_{z}", (5.8, 3.6, z + 7), (1.0, 0.78, 0.48), 220.0)

    # Barricada y protesta al fondo de Av. Juarez
    for i in range(-2, 3):
        import_fbx(PROPS_DIR, "SM_Valla_Bloqueo.fbx", (i * 2.45, 0.0, 79.6), 0)
    import_fbx(PROPS_DIR, "SM_Cono_Obra.fbx", (-6.2, 0.0, 78.8), 0)
    import_fbx(PROPS_DIR, "SM_Cono_Obra.fbx", (6.2, 0.0, 78.8), 0)
    import_fbx(PROPS_DIR, "SM_Linea_Policia.fbx", (0.0, 0.0, 83.2), 0)
    import_fbx(PROPS_DIR, "SM_Multitud_Protesta.fbx", (0.0, 0.0, 89.0), 0)
    import_fbx(PROPS_DIR, "SM_Multitud_Protesta.fbx", (-13.0, 0.0, 88.5), 18)
    import_fbx(PROPS_DIR, "SM_Multitud_Protesta.fbx", (13.0, 0.0, 88.5), -18)
    import_fbx(PROPS_DIR, "SM_Patrulla.fbx", (-2.6, 0.0, 85.4), 28)
    import_fbx(PROPS_DIR, "SM_Patrulla.fbx", (4.5, 0.0, 85.8), -22)
    import_fbx(PROPS_DIR, "SM_Semaforo_GDL.fbx", (5.35, 0.18, 76.2), 0)
    import_fbx(PROPS_DIR, "SM_Semaforo_GDL.fbx", (-5.35, 0.18, 18.0), 180)


def render_03_av_juarez():
    reset_scene()
    build_av_juarez_base()

    # Trafico detenido variado
    import_fbx(PROPS_DIR, "SM_Auto_Urbano.fbx", (-3.2, 0.0, 33.5), 176, ("Car_Taxi", (0.94, 0.72, 0.08)))
    import_fbx(PROPS_DIR, "SM_Auto_Urbano.fbx", (3.2, 0.0, 41.0), 184, ("Car_Red", (0.62, 0.11, 0.11)))
    import_fbx(PROPS_DIR, "SM_Camion_Urbano.fbx", (-2.8, 0.0, 52.0), 180)
    import_fbx(PROPS_DIR, "SM_Auto_Urbano.fbx", (3.2, 0.0, 62.5), 168, ("Car_Grey", (0.28, 0.31, 0.35)))
    import_fbx(PROPS_DIR, "SM_Auto_Urbano.fbx", (-3.2, 0.0, 68.0), 182, ("Car_Blue", (0.14, 0.25, 0.48)))
    import_fbx(PROPS_DIR, "SM_Moto_Reparto.fbx", (4.4, 0.0, 26.5), 165)

    # Mobiliario y arboles
    import_fbx(PROPS_DIR, "SM_Puesto_Periodicos.fbx", (-6.5, 0.18, 29.0), -90)
    import_fbx(PROPS_DIR, "SM_Parada_Camion.fbx", (6.4, 0.18, 36.0), 90)
    for pos, yaw in [((-6.5, 0.18, 43.0), -90), ((6.5, 0.18, 51.0), 90), ((-6.5, 0.18, 58.0), -90)]:
        import_fbx(PROPS_DIR, "SM_Banca_Colonial.fbx", pos, yaw)
    for pos in [(6.35, 0.18, 30.5), (-6.35, 0.18, 49.5), (6.35, 0.18, 60.5), (-6.35, 0.18, 66.5)]:
        import_fbx(PROPS_DIR, "SM_Arbol_Jardinera.fbx", pos, 0)

    # Peatones y manifestantes
    add_civilian("Ped1", (-5.6, 0.18, 31.0), 175, (0.22, 0.30, 0.48), backpack=True)
    add_civilian("Ped2", (5.6, 0.18, 33.5), 185, (0.55, 0.18, 0.18))
    add_civilian("Ped3", (-5.5, 0.18, 44.5), 15, (0.65, 0.55, 0.38), placard=True)
    add_civilian("Ped4", (5.5, 0.18, 46.5), -10, (0.25, 0.38, 0.28), phone=True)
    add_civilian("Ped5", (4.5, 0.0, 56.0), 5, (0.52, 0.34, 0.42), placard=True)
    add_civilian("Ped6", (-5.7, 0.18, 59.0), 178, (0.26, 0.26, 0.29), backpack=True)

    setup_sunset_world_and_lights(sun_euler=(62, 0, -135), sun_energy=4.0)
    add_point_light("Fill_Street1", (0.0, 4.5, 36.0), (0.85, 0.90, 1.0), 350.0, 2.0)
    add_point_light("Fill_Street2", (0.0, 5.0, 56.0), (1.0, 0.85, 0.65), 400.0, 2.0)
    add_point_light("Strobe_R", (-2.8, 2.5, 85.0), (1.0, 0.15, 0.12), 450.0, 0.5)
    add_point_light("Strobe_B", (4.2, 2.5, 85.0), (0.15, 0.38, 1.0), 450.0, 0.5)

    enhance_materials()
    render_view("03_av_juarez_trafico_y_edificios.png", (1.2, 2.35, 24.5), (0.0, 2.6, 62.0), lens=26.0)


def render_04_muralla():
    reset_scene()
    build_av_juarez_base()
    import_fbx(PROPS_DIR, "SM_Arbol_Jardinera.fbx", (6.35, 0.18, 60.5), 0)
    import_fbx(PROPS_DIR, "SM_Arbol_Jardinera.fbx", (-6.35, 0.18, 66.5), 0)
    add_civilian("Bystander_L", (-5.7, 0.18, 74.5), 25, (0.22, 0.32, 0.46), phone=True)
    add_civilian("Bystander_R", (5.5, 0.18, 72.5), -30, (0.62, 0.52, 0.38), phone=True)

    setup_sunset_world_and_lights(sun_euler=(65, 0, -145), sun_energy=3.8)
    add_point_light("FrontBarricadeFill", (-1.0, 3.8, 73.5), (1.0, 0.88, 0.76), 520.0, 1.5)
    add_point_light("PoliceFill", (0.0, 4.2, 81.5), (0.95, 0.96, 1.0), 450.0, 1.5)
    add_point_light("Strobe_R", (-2.6, 2.6, 85.0), (1.0, 0.12, 0.10), 650.0, 0.6)
    add_point_light("Strobe_B", (4.5, 2.6, 85.0), (0.15, 0.40, 1.0), 650.0, 0.6)

    enhance_materials()
    render_view("04_muralla_barricada_y_manifestacion.png", (-2.6, 2.70, 71.2), (0.5, 1.85, 85.2), lens=28.0)


def render_05_floreria():
    reset_scene()
    import_fbx(ARCH_DIR, "SM_Ruta_Piso_Completo.fbx")
    import_fbx(ARCH_DIR, "SM_Manzanas_Hospital_Floreria.fbx")
    import_fbx(PROPS_DIR, "SM_Auto_Urbano.fbx", (67.5, 0.0, 11.4), -88, ("Car_Blue", (0.14, 0.25, 0.48)))
    import_fbx(PROPS_DIR, "SM_Auto_Urbano.fbx", (78.5, 0.0, 18.6), 92, ("Car_Red", (0.62, 0.11, 0.11)))
    import_fbx(PROPS_DIR, "SM_Moto_Reparto.fbx", (89.2, 0.0, 11.2), -65)
    import_fbx(PROPS_DIR, "SM_Parada_Camion.fbx", (72.0, 0.18, 8.6), 180)
    import_fbx(PROPS_DIR, "SM_Banca_Colonial.fbx", (80.0, 0.18, 8.6), 180)
    import_fbx(PROPS_DIR, "SM_Banca_Colonial.fbx", (76.0, 0.18, 21.4), 0)
    for pos in [(68.0, 0.18, 21.3), (82.5, 0.18, 21.3), (86.0, 0.18, 8.7)]:
        import_fbx(PROPS_DIR, "SM_Arbol_Jardinera.fbx", pos, 0)
    for x in [64, 77, 90]:
        import_fbx(PROPS_DIR, "SM_Farola_Colonial_GDL.fbx", (x, 0.18, 21.7), 90)
        import_fbx(PROPS_DIR, "SM_Farola_Colonial_GDL.fbx", (x + 3, 0.18, 8.3), -90)

    add_civilian("Florist", (93.2, 0.18, 17.8), -90, (0.15, 0.52, 0.30))
    add_civilian("Shopper1", (81.0, 0.18, 20.5), 85, (0.55, 0.18, 0.18), backpack=True)
    add_civilian("Shopper2", (76.5, 0.18, 9.5), -90, (0.20, 0.28, 0.44))

    setup_sunset_world_and_lights(sun_euler=(58, 0, -40), sun_energy=4.4)
    add_point_light("FloreriaBeacon", (92.8, 3.8, 15.0), (0.85, 1.0, 0.78), 500.0, 1.2)
    add_point_light("HospitalFill", (79.0, 4.2, 15.0), (1.0, 0.88, 0.72), 450.0, 2.0)

    enhance_materials()
    render_view("05_floreria_el_jazmin_y_calle_hospital.png", (73.5, 2.5, 16.2), (94.8, 2.7, 14.8), lens=28.0)


def build_studio_stage():
    bpy.ops.mesh.primitive_plane_add(size=80.0, location=(0, 0, 0))
    floor = bpy.context.active_object
    floor.data.materials.append(make_mat("StudioFloor", (0.16, 0.17, 0.19), roughness=0.65))

    world = bpy.data.worlds.new("StudioWorld")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs["Color"].default_value = (0.18, 0.20, 0.24, 1.0)
        bg.inputs["Strength"].default_value = 1.4

    sun_d = bpy.data.lights.new("KeySun", type='SUN')
    sun_d.energy = 3.8
    sun_d.color = (1.0, 0.94, 0.86)
    sun_o = bpy.data.objects.new("KeySun", sun_d)
    bpy.context.scene.collection.objects.link(sun_o)
    sun_o.rotation_euler = (math.radians(48), 0, math.radians(-140))

    add_point_light("StudioFillL", (9.0, 6.0, -8.0), (0.80, 0.88, 1.0), 900.0, 3.0)
    add_point_light("StudioFillR", (-9.0, 6.0, -8.0), (1.0, 0.86, 0.72), 900.0, 3.0)
    add_point_light("StudioRim", (0.0, 6.0, 12.0), (1.0, 0.96, 0.90), 800.0, 3.0)


def render_06_catalogo_vehiculos():
    reset_scene()
    build_studio_stage()

    # Fila delantera mirando hacia camara (-Z = yaw 0..35)
    import_fbx(PROPS_DIR, "SM_Auto_Urbano.fbx", (-6.2, 0.0, -1.5), 30, ("Cat_Taxi", (0.94, 0.72, 0.08)))
    import_fbx(PROPS_DIR, "SM_Auto_Urbano.fbx", (-2.6, 0.0, -1.5), 25, ("Cat_Red", (0.62, 0.11, 0.11)))
    import_fbx(PROPS_DIR, "SM_Moto_Reparto.fbx", (0.2, 0.0, -1.3), 28)
    import_fbx(PROPS_DIR, "SM_Valla_Bloqueo.fbx", (3.0, 0.0, -1.2), 12)
    import_fbx(PROPS_DIR, "SM_Banca_Colonial.fbx", (6.0, 0.0, -1.2), -15)
    import_fbx(PROPS_DIR, "SM_Cono_Obra.fbx", (8.2, 0.0, -1.2), 0)

    # Fila trasera mirando hacia camara (-Z)
    import_fbx(PROPS_DIR, "SM_Camion_Urbano.fbx", (-6.8, 0.0, 5.8), 35)
    import_fbx(PROPS_DIR, "SM_Patrulla.fbx", (-2.2, 0.0, 5.4), 25)
    import_fbx(PROPS_DIR, "SM_Puesto_Periodicos.fbx", (1.4, 0.0, 5.4), 10)
    import_fbx(PROPS_DIR, "SM_Parada_Camion.fbx", (5.4, 0.0, 5.4), -12)
    import_fbx(PROPS_DIR, "SM_Arbol_Jardinera.fbx", (9.2, 0.0, 5.2), 20)
    import_fbx(PROPS_DIR, "SM_Semaforo_GDL.fbx", (11.6, 0.0, 2.8), -10)
    import_fbx(PROPS_DIR, "SM_Farola_Colonial_GDL.fbx", (-9.6, 0.0, 2.2), 0)

    enhance_materials()
    render_view("06_catalogo_vehiculos_y_mobiliario.png", (0.4, 6.4, -17.0), (0.6, 1.5, 2.2), lens=28.0)


def render_07_catalogo_personajes():
    reset_scene()
    build_studio_stage()

    import_fbx(PROPS_DIR, "SM_Linea_Policia.fbx", (-3.4, 0.0, 4.6), 12)
    import_fbx(PROPS_DIR, "SM_Multitud_Protesta.fbx", (3.8, 0.0, 5.6), -12)

    # 5 civiles articulados al frente mirando hacia la camara (yaw=180 mira hacia -Z)
    add_civilian("Civ_1", (-4.4, 0.0, -1.2), 170, (0.20, 0.28, 0.44), backpack=True)
    add_civilian("Civ_2", (-2.2, 0.0, -1.2), 175, (0.55, 0.18, 0.18), placard=True)
    add_civilian("Civ_3", (0.0, 0.0, -1.2), 180, (0.66, 0.56, 0.38), phone=True)
    add_civilian("Civ_4", (2.2, 0.0, -1.2), 185, (0.15, 0.52, 0.30))
    add_civilian("Civ_5", (4.4, 0.0, -1.2), 190, (0.52, 0.34, 0.42), backpack=True)

    enhance_materials()
    render_view("07_catalogo_personajes_y_multitud.png", (0.0, 3.0, -12.0), (0.0, 1.35, 2.6), lens=28.0)


if __name__ == "__main__":
    render_03_av_juarez()
    render_04_muralla()
    render_05_floreria()
    render_06_catalogo_vehiculos()
    render_07_catalogo_personajes()
    print("[OK] 5 renders de galeria completados en", OUT_DIR)
