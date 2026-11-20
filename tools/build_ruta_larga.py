# -*- coding: utf-8 -*-
"""
Ruta Segura - Generador rapido de la ruta larga (replica del video de referencia).

Recorrido (coordenadas Unity: X = derecha, Y = arriba, Z = adelante):
  1. Av. Juarez       : calle recta de Z -42 a Z 124. Jugador inicia en Z 22.
  2. Barricada        : Z 79.6, cruce con Av. Hidalgo (Z 82..94) con manifestantes y policia.
  3. Callejon 1       : sale a la derecha en Z 70..75, de X 7.5 a X 45. Al fondo (Calle Galeana)
                        hay una linea de policia y multitud que bloquea el paso.
  4. Callejon 2       : baja hacia el sur desde el Callejon 1 en X 27..32, de Z 70 a Z 22.5.
  5. Calle Hospital   : Z 10..20, de X 18 a X 95. Cruce con conos y cinta.
  6. Destino          : Floreria El Jazmin al fondo de Calle Hospital (frente en X 95).

Cada malla se construye con bmesh mapeando coordenadas Unity (Ux, Uy, Uz) a Blender
(-Ux, -Uz, +Uy) y se exporta con bake_space_transform=True, axis_forward='-Z', axis_up='Y'.
Asi Unity importa cada FBX con Pos(0,0,0), Rot(0,0,0), Scale(1,1,1) positivos exactos.
"""
import json
import math
import os
import random

import bmesh
import bpy
from mathutils import Matrix, Vector

ROOT = r"C:\Users\luisk\Ruta-Segura-\RutaSegura\Assets\_Project\Models"
ARCH_DIR = os.path.join(ROOT, "Architecture")
PROPS_DIR = os.path.join(ROOT, "Props")
BOUNDS_JSON = os.path.join(ROOT, "layout_bounds.json")
os.makedirs(ARCH_DIR, exist_ok=True)
os.makedirs(PROPS_DIR, exist_ok=True)

BOUNDS = []


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def mat(name, color, roughness=0.7, metallic=0.0):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name=name)
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs['Base Color'].default_value = (color[0], color[1], color[2], 1.0)
            bsdf.inputs['Roughness'].default_value = roughness
            bsdf.inputs['Metallic'].default_value = metallic
        m.diffuse_color = (color[0], color[1], color[2], 1.0)
    return m


def mats():
    return {
        # Muros (paleta del centro de Guadalajara)
        'cantera': mat("M_Muro_Cantera", (0.82, 0.74, 0.60)),
        'terracota': mat("M_Muro_Terracota", (0.68, 0.32, 0.22)),
        'amarillo': mat("M_Muro_AmarilloColonial", (0.85, 0.68, 0.28)),
        'blanco': mat("M_Muro_BlancoColonial", (0.88, 0.86, 0.82)),
        'ocre': mat("M_Muro_Ocre", (0.72, 0.52, 0.30)),
        'rosa': mat("M_Muro_RosaDeslavado", (0.78, 0.52, 0.52)),
        'verde': mat("M_Muro_VerdeAgua", (0.52, 0.66, 0.60)),
        'azul': mat("M_Muro_AzulAnil", (0.40, 0.50, 0.62)),
        'gris': mat("M_Muro_GrisCemento", (0.55, 0.54, 0.52)),
        'ladrillo': mat("M_Muro_Ladrillo", (0.48, 0.24, 0.18)),
        'ladrillo_osc': mat("M_Muro_LadrilloOscuro", (0.34, 0.20, 0.16)),
        # Detalles
        'cortina': mat("M_Cortina_Metalica", (0.28, 0.30, 0.33), 0.45, 0.7),
        'herreria': mat("M_Herreria_Negra", (0.08, 0.08, 0.10), 0.55, 0.8),
        'madera': mat("M_Madera_Puerta", (0.24, 0.14, 0.08), 0.5),
        'vidrio': mat("M_Vidrio_Ventana", (0.10, 0.13, 0.16), 0.15, 0.6),
        'cristal': mat("M_Cristal_Aparador", (0.15, 0.25, 0.30), 0.1, 0.9),
        'minisplit': mat("M_Minisplit", (0.82, 0.82, 0.80), 0.5),
        'tinaco': mat("M_Tinaco_Negro", (0.10, 0.10, 0.11), 0.6),
        'toldo_r': mat("M_Toldo_Rojo", (0.70, 0.18, 0.16), 0.7),
        'toldo_v': mat("M_Toldo_Verde", (0.16, 0.42, 0.26), 0.7),
        'toldo_a': mat("M_Toldo_Azul", (0.16, 0.26, 0.48), 0.7),
        'pinta_1': mat("M_Pinta_Grafiti_1", (0.20, 0.22, 0.30), 0.8),
        'pinta_2': mat("M_Pinta_Grafiti_2", (0.62, 0.62, 0.64), 0.8),
        'pinta_3': mat("M_Pinta_Grafiti_3", (0.30, 0.12, 0.14), 0.8),
        'toldo': mat("M_Toldo_Floreria", (0.80, 0.22, 0.20), 0.6),
        'floreria': mat("M_Floreria_Destino", (0.18, 0.58, 0.32), 0.4),
        # Piso
        'asfalto': mat("M_Asfalto_Calle", (0.16, 0.16, 0.18), 0.85),
        'concreto_callejon': mat("M_Concreto_Callejon", (0.30, 0.29, 0.28), 0.9),
        'banqueta': mat("M_Concreto_Banqueta", (0.60, 0.60, 0.62), 0.8),
        'machuelo': mat("M_Machuelo_Guarnicion", (0.75, 0.75, 0.72), 0.7),
        'linea_blanca': mat("M_Paso_Cebra_Blanco", (0.85, 0.85, 0.82), 0.6),
        'linea_amarilla': mat("M_Linea_Amarilla", (0.85, 0.66, 0.12), 0.6),
        'coladera': mat("M_Coladera_Hierro", (0.12, 0.12, 0.12), 0.5, 0.8),
        # Personajes y vehiculos
        'ropa_1': mat("M_Ropa_Gris", (0.32, 0.32, 0.34)),
        'ropa_2': mat("M_Ropa_Azul", (0.18, 0.24, 0.38)),
        'ropa_3': mat("M_Ropa_Vino", (0.40, 0.14, 0.16)),
        'ropa_4': mat("M_Ropa_Beige", (0.62, 0.56, 0.44)),
        'ropa_5': mat("M_Ropa_Negra", (0.10, 0.10, 0.11)),
        'piel_1': mat("M_Piel_Morena", (0.52, 0.36, 0.26)),
        'piel_2': mat("M_Piel_Clara", (0.72, 0.56, 0.44)),
        'pantalon': mat("M_Pantalon_Mezclilla", (0.16, 0.20, 0.30)),
        'cartulina': mat("M_Cartulina_Pancarta", (0.92, 0.90, 0.84)),
        'policia': mat("M_Uniforme_Policia", (0.08, 0.10, 0.16), 0.6),
        'escudo': mat("M_Escudo_Policia", (0.30, 0.34, 0.38), 0.2, 0.3),
        'casco': mat("M_Casco_Policia", (0.06, 0.06, 0.08), 0.3, 0.4),
        'patrulla_b': mat("M_Patrulla_Blanco", (0.86, 0.86, 0.88), 0.3, 0.4),
        'patrulla_n': mat("M_Patrulla_Negro", (0.06, 0.07, 0.10), 0.3, 0.4),
        'torreta_r': mat("M_Torreta_Roja", (0.85, 0.08, 0.08), 0.2),
        'torreta_a': mat("M_Torreta_Azul", (0.08, 0.20, 0.85), 0.2),
        'llanta': mat("M_Llanta", (0.05, 0.05, 0.05), 0.9),
        'contenedor': mat("M_Contenedor_Verde", (0.16, 0.30, 0.20), 0.6, 0.3),
        'bolsa': mat("M_Bolsa_Basura", (0.05, 0.05, 0.06), 0.4),
        'cono': mat("M_Cono_Trafico", (0.95, 0.40, 0.05), 0.5),
        'cinta': mat("M_Cinta_Precaucion", (0.95, 0.80, 0.08), 0.5),
        'poste': mat("M_Poste_Gris", (0.40, 0.40, 0.42), 0.5, 0.6),
        'foco': mat("M_Foco_Luz", (1.0, 0.92, 0.70), 0.3),
        # Valla de bloqueo y senaletica
        'metal_valla': mat("M_Metal_Valla", (0.75, 0.75, 0.78), 0.4, 0.6),
        'placa_cerrada': mat("M_Placa_CalleCerrada", (0.96, 0.73, 0.08), 0.45),
        'placa_desvio': mat("M_Placa_Desvio", (0.96, 0.73, 0.08), 0.45),
    }


class MeshBuilder:
    """Constructor bmesh unico por archivo FBX."""

    def __init__(self):
        self.bm = bmesh.new()
        self.materials = []
        self.mat_idx = {}
        self.uv_layer = self.bm.loops.layers.uv.new("UVMap")

    def mat_slot(self, material):
        idx = self.mat_idx.get(material.name)
        if idx is None:
            idx = len(self.materials)
            self.materials.append(material)
            self.mat_idx[material.name] = idx
        return idx

    def add_box(self, size, pos, material, uv_plate=False):
        sx, sy, sz = size
        if sx <= 0.001 or sy <= 0.001 or sz <= 0.001:
            return
        ux, uy, uz = pos
        ret = bmesh.ops.create_cube(self.bm, size=1.0)
        verts = ret['verts']
        for v in verts:
            # Unity (Ux, Uy, Uz) -> Blender (-Ux, -Uz, +Uy) con escala positiva
            v.co.x = v.co.x * sx - ux
            v.co.y = v.co.y * sz - uz
            v.co.z = v.co.z * sy + uy
        mi = self.mat_slot(material)
        faces = set(f for v in verts for f in v.link_faces)
        ux_min = ux - sx * 0.5
        uy_min = uy - sy * 0.5
        for f in faces:
            f.material_index = mi
            if uv_plate:
                for loop in f.loops:
                    vx_u = -loop.vert.co.x
                    vy_u = loop.vert.co.z
                    u = max(0.0, min(1.0, (vx_u - ux_min) / sx))
                    v_coord = max(0.0, min(1.0, (vy_u - uy_min) / sy))
                    loop[self.uv_layer].uv = (u, v_coord)

    def add_cylinder(self, radius, depth, pos, material, axis='Y', rot_x_rad=0.0, segments=12):
        """Cilindro en coordenadas Unity; axis 'Y'=vertical, 'X'=horizontal ancho, 'Z'=profundo."""
        ux, uy, uz = pos
        ret = bmesh.ops.create_cone(
            self.bm,
            cap_ends=True,
            cap_tris=False,
            segments=segments,
            radius1=radius,
            radius2=radius,
            depth=depth,
        )
        verts = ret['verts']
        rot = Matrix.Identity(3)
        if axis == 'X':
            rot = Matrix.Rotation(math.pi / 2.0, 3, 'Y')
        elif axis == 'Z':
            rot = Matrix.Rotation(math.pi / 2.0, 3, 'X')
        if abs(rot_x_rad) > 1e-6:
            rot = Matrix.Rotation(rot_x_rad, 3, 'X') @ rot
        offset = Vector((-ux, -uz, uy))
        for v in verts:
            v.co = rot @ v.co + offset
        mi = self.mat_slot(material)
        for f in set(f for v in verts for f in v.link_faces):
            f.material_index = mi


def box(mb, size, pos, material, name="box"):
    mb.add_box(size, pos, material)


def export(mb, filename, folder):
    mesh_name = os.path.splitext(filename)[0]
    mesh = bpy.data.meshes.new(mesh_name)
    mb.bm.normal_update()
    mb.bm.to_mesh(mesh)
    mb.bm.free()
    for m in mb.materials:
        mesh.materials.append(m)

    obj = bpy.data.objects.new(mesh_name, mesh)
    bpy.context.collection.objects.link(obj)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    # Limites en coordenadas exactas de Unity: Ux = -Bx, Uy = Bz, Uz = -By
    bxs = [v.co.x for v in mesh.vertices]
    bys = [v.co.y for v in mesh.vertices]
    bzs = [v.co.z for v in mesh.vertices]
    umin = {"x": -max(bxs), "y": min(bzs), "z": -max(bys)}
    umax = {"x": -min(bxs), "y": max(bzs), "z": -min(bys)}
    BOUNDS.append({"name": mesh_name, "min": umin, "max": umax})

    bpy.ops.export_scene.fbx(
        filepath=os.path.join(folder, filename),
        use_selection=True,
        axis_forward='-Z',
        axis_up='Y',
        use_space_transform=True,
        bake_space_transform=True,
        apply_scale_options='FBX_SCALE_ALL',
        object_types={'MESH'},
    )
    print(f"[EXPORT] {filename}  Unity X {umin['x']:.1f}..{umax['x']:.1f}  "
          f"Y {umin['y']:.2f}..{umax['y']:.2f}  Z {umin['z']:.1f}..{umax['z']:.1f}  "
          f"tris~{len(mesh.polygons) * 2}")


# ------------------------------------------------------------------------------
# Fachadas: una "fila" de edificios a lo largo de una calle
# ------------------------------------------------------------------------------
class Row:
    def __init__(self, mb, axis, front, facing):
        self.mb = mb
        self.axis = axis
        self.front = front
        self.facing = facing

    def fbox(self, u, y, o, su, sy, so, material, name="f"):
        n = self.front + self.facing * o
        if self.axis == 'z':
            self.mb.add_box((so, sy, su), (n, y, u), material)
        else:
            self.mb.add_box((su, sy, so), (u, y, n), material)


WALLS_MAIN = ['cantera', 'terracota', 'amarillo', 'blanco', 'ocre', 'rosa', 'verde', 'azul', 'gris']
WALLS_ALLEY = ['ladrillo', 'ladrillo_osc', 'gris', 'terracota', 'ocre', 'ladrillo']


def building(row, m, rng, u0, w, style, depth):
    alley = style == 'callejon'
    h = rng.choice([9.0, 10.5, 12.0, 13.5, 15.0]) if alley else rng.choice([8.5, 10.0, 11.5, 13.0, 14.5, 16.0])
    wall = m[rng.choice(WALLS_ALLEY if alley else WALLS_MAIN)]
    uc = u0 + w / 2.0

    row.fbox(uc, h / 2.0, -depth / 2.0, w, h, depth, wall)
    row.fbox(uc, 0.3, 0.06, w, 0.6, 0.12, m['cantera'] if not alley else m['gris'])
    row.fbox(uc, h - 0.25, 0.2, w, 0.5, 0.4, m['cantera'])
    if rng.random() < 0.6:
        row.fbox(u0 + rng.uniform(1.0, max(1.1, w - 1.0)), h + 0.65, -rng.uniform(1.5, 3.5), 1.1, 1.3, 1.1, m['tinaco'])

    gstyle = rng.choice(['cortina', 'cortina', 'porton', 'aparador'] if not alley else ['cortina', 'puerta', 'muro'])
    if gstyle == 'cortina':
        cw = min(w - 1.2, 4.2)
        row.fbox(uc, 1.45, 0.05, cw, 2.9, 0.10, m['cortina'])
        row.fbox(uc, 3.1, 0.08, cw + 0.4, 0.4, 0.16, m['cantera'])
        if not alley and rng.random() < 0.5:
            toldo = m[rng.choice(['toldo_r', 'toldo_v', 'toldo_a'])]
            row.fbox(uc, 3.45, 0.6, cw + 0.4, 0.08, 1.2, toldo)
    elif gstyle == 'porton':
        row.fbox(uc - w * 0.2, 1.6, 0.05, 1.9, 3.2, 0.10, m['madera'])
        row.fbox(uc - w * 0.2, 1.7, 0.03, 2.3, 3.4, 0.06, m['cantera'])
        row.fbox(uc + w * 0.22, 1.7, 0.10, 1.4, 1.8, 0.20, m['herreria'])
    elif gstyle == 'aparador':
        aw = min(w - 1.5, 4.0)
        row.fbox(uc, 1.7, 0.05, aw, 2.4, 0.10, m['cristal'])
        row.fbox(uc, 1.7, 0.03, aw + 0.3, 2.7, 0.06, m['cantera'])
    elif gstyle == 'puerta':
        row.fbox(uc + rng.uniform(-w * 0.25, w * 0.25), 1.2, 0.05, 1.1, 2.4, 0.10, m['herreria'])

    if alley or rng.random() < 0.25:
        for _ in range(rng.randint(1, 3) if alley else 1):
            gw = rng.uniform(1.2, 3.0)
            gh = rng.uniform(0.7, 1.5)
            gu = u0 + rng.uniform(gw / 2, max(gw / 2 + 0.01, w - gw / 2))
            row.fbox(gu, 0.7 + gh / 2 + rng.uniform(0, 0.8), 0.02, gw, gh, 0.03,
                     m[rng.choice(['pinta_1', 'pinta_2', 'pinta_3'])])

    floors = int((h - 3.8) // 3.2)
    nwin = max(1, int(w // 2.6))
    spacing = w / nwin
    for f in range(floors):
        yb = 3.8 + f * 3.2
        for i in range(nwin):
            u = u0 + spacing * (i + 0.5)
            row.fbox(u, yb + 1.15, 0.06, 1.3, 2.0, 0.12, m['cantera'])
            row.fbox(u, yb + 1.15, 0.07, 1.0, 1.7, 0.14, m['vidrio'])
            r = rng.random()
            if not alley and r < 0.35:
                row.fbox(u, yb + 0.08, 0.32, 1.7, 0.16, 0.64, m['cantera'])
                row.fbox(u, yb + 0.6, 0.62, 1.7, 0.9, 0.04, m['herreria'])
            elif alley and r < 0.4:
                row.fbox(u + 0.95, yb + 0.5, 0.16, 0.8, 0.55, 0.32, m['minisplit'])
            elif alley and r < 0.55:
                row.fbox(u, yb + 1.0, 0.10, 1.2, 1.8, 0.20, m['herreria'])


def street_row(mb, m, axis, front, a0, a1, facing, style='main', seed=0, depth=8.0):
    rng = random.Random(seed)
    row = Row(mb, axis, front, facing)
    u = a0
    while u < a1 - 0.01:
        w = rng.uniform(5.0, 9.0)
        if a1 - (u + w) < 4.0:
            w = a1 - u
        building(row, m, rng, u, w, style, depth)
        u += w


# ------------------------------------------------------------------------------
# 1. Piso: asfalto, banquetas, callejones, senalizacion
# ------------------------------------------------------------------------------
def sidewalk(p, m, x0, x1, z0, z1, curb):
    box(p, (x1 - x0, 0.18, z1 - z0), ((x0 + x1) / 2, 0.09, (z0 + z1) / 2), m['banqueta'])
    c = 0.16
    if curb == 'x0':
        box(p, (c, 0.19, z1 - z0), (x0 + c / 2, 0.095, (z0 + z1) / 2), m['machuelo'])
    elif curb == 'x1':
        box(p, (c, 0.19, z1 - z0), (x1 - c / 2, 0.095, (z0 + z1) / 2), m['machuelo'])
    elif curb == 'z0':
        box(p, (x1 - x0, 0.19, c), ((x0 + x1) / 2, 0.095, z0 + c / 2), m['machuelo'])
    elif curb == 'z1':
        box(p, (x1 - x0, 0.19, c), ((x0 + x1) / 2, 0.095, z1 - c / 2), m['machuelo'])


def make_piso():
    reset()
    m = mats()
    p = MeshBuilder()
    box(p, (185.0, 0.3, 190.0), (20.0, -0.15, 42.0), m['asfalto'])

    box(p, (37.5, 0.01, 5.0), (26.25, 0.005, 72.5), m['concreto_callejon'])
    box(p, (5.0, 0.01, 47.5), (29.5, 0.005, 46.25), m['concreto_callejon'])
    for (x, z) in [(0.0, -10.0), (2.5, 30.0), (-2.0, 58.0), (20.0, 72.5), (29.5, 50.0), (29.5, 32.0), (60.0, 15.0)]:
        box(p, (0.7, 0.012, 0.7), (x, 0.006, z), m['coladera'])

    # Banquetas Av. Juarez
    sidewalk(p, m, -7.5, -5.0, -42.0, 82.0, 'x1')
    sidewalk(p, m, -7.5, -5.0, 94.0, 124.0, 'x1')
    sidewalk(p, m, 5.0, 7.5, -42.0, 70.0, 'x0')
    sidewalk(p, m, 5.0, 7.5, 75.0, 82.0, 'x0')
    sidewalk(p, m, 5.0, 7.5, 94.0, 124.0, 'x0')
    # Banquetas Av. Hidalgo
    sidewalk(p, m, -62.0, -7.5, 82.0, 84.5, 'z1')
    sidewalk(p, m, 7.5, 37.0, 82.0, 84.5, 'z1')
    sidewalk(p, m, -62.0, -7.5, 91.5, 94.0, 'z0')
    sidewalk(p, m, 7.5, 37.0, 91.5, 94.0, 'z0')
    # Banquetas Calle Galeana
    sidewalk(p, m, 45.0, 47.5, 40.0, 70.0, 'x1')
    sidewalk(p, m, 45.0, 47.5, 75.0, 100.0, 'x1')
    sidewalk(p, m, 52.5, 55.0, 40.0, 100.0, 'x0')
    # Banquetas Calle Hospital y frente a Floreria El Jazmin
    sidewalk(p, m, 18.0, 27.0, 20.0, 22.5, 'z0')
    sidewalk(p, m, 32.0, 95.0, 20.0, 22.5, 'z0')
    sidewalk(p, m, 18.0, 95.0, 7.5, 10.0, 'z1')
    sidewalk(p, m, 92.5, 95.0, 10.0, 20.0, 'x0')

    # Senalizacion horizontal
    z = -38.0
    while z < 74.0:
        box(p, (0.15, 0.01, 3.0), (0.0, 0.005, z + 1.5), m['linea_blanca'])
        z += 7.0
    box(p, (0.12, 0.01, 112.0), (-4.7, 0.005, 14.0), m['linea_amarilla'])
    box(p, (0.12, 0.01, 112.0), (4.7, 0.005, 14.0), m['linea_amarilla'])
    box(p, (10.0, 0.01, 0.4), (0.0, 0.005, 76.2), m['linea_blanca'])
    for i in range(8):
        box(p, (0.6, 0.01, 3.0), (-4.2 + i * 1.2, 0.005, 78.8), m['linea_blanca'])
    for z in [20.0, 50.0]:
        for x in [-2.5, 2.5]:
            box(p, (0.25, 0.01, 2.2), (x, 0.005, z), m['linea_blanca'])
            box(p, (0.9, 0.01, 0.5), (x, 0.005, z + 1.3), m['linea_blanca'])
    x = 36.0
    while x < 92.0:
        box(p, (3.0, 0.01, 0.15), (x + 1.5, 0.005, 15.0), m['linea_blanca'])
        x += 7.0
    for i in range(5):
        box(p, (3.0, 0.01, 0.6), (29.5, 0.005, 11.0 + i * 1.8), m['linea_blanca'])
    z = 42.0
    while z < 98.0:
        box(p, (0.15, 0.01, 3.0), (50.0, 0.005, z + 1.5), m['linea_blanca'])
        z += 7.0

    export(p, "SM_Ruta_Piso_Completo.fbx", ARCH_DIR)


# ------------------------------------------------------------------------------
# 2. Manzanas de Av. Juarez y Av. Hidalgo
# ------------------------------------------------------------------------------
def make_manzanas_juarez():
    reset()
    m = mats()
    p = MeshBuilder()
    street_row(p, m, 'z', -7.5, -42.0, 82.0, +1, 'main', 11)
    street_row(p, m, 'z', -7.5, 94.0, 124.0, +1, 'main', 12)
    street_row(p, m, 'z', 7.5, -42.0, 70.0, -1, 'main', 13)
    street_row(p, m, 'z', 7.5, 75.0, 82.0, -1, 'main', 14)
    street_row(p, m, 'z', 7.5, 94.0, 124.0, -1, 'main', 15)
    street_row(p, m, 'x', -42.0, -15.5, 15.5, +1, 'main', 16)
    street_row(p, m, 'x', 124.0, -15.5, 15.5, -1, 'main', 17)
    street_row(p, m, 'x', 82.0, -62.0, -15.5, +1, 'main', 18)
    street_row(p, m, 'x', 94.0, -62.0, -15.5, -1, 'main', 19)
    street_row(p, m, 'x', 94.0, 15.5, 37.0, -1, 'main', 20)
    street_row(p, m, 'z', -62.0, 82.0, 94.0, +1, 'main', 21)
    export(p, "SM_Manzanas_Juarez_Hidalgo.fbx", ARCH_DIR)


# ------------------------------------------------------------------------------
# 3. Callejones y Calle Galeana
# ------------------------------------------------------------------------------
def make_manzanas_callejones():
    reset()
    m = mats()
    p = MeshBuilder()
    street_row(p, m, 'x', 70.0, 15.5, 27.0, +1, 'callejon', 31)
    street_row(p, m, 'x', 70.0, 32.0, 37.0, +1, 'callejon', 32)
    street_row(p, m, 'x', 75.0, 15.5, 37.0, -1, 'callejon', 33, depth=7.0)
    street_row(p, m, 'z', 27.0, 30.5, 62.0, +1, 'callejon', 34)
    street_row(p, m, 'z', 32.0, 30.5, 62.0, -1, 'callejon', 35)
    street_row(p, m, 'z', 45.0, 40.0, 70.0, +1, 'main', 36)
    street_row(p, m, 'z', 45.0, 75.0, 100.0, +1, 'main', 37)
    street_row(p, m, 'z', 55.0, 40.0, 100.0, -1, 'main', 38)
    street_row(p, m, 'x', 40.0, 37.0, 63.0, +1, 'main', 39)
    street_row(p, m, 'x', 100.0, 37.0, 63.0, -1, 'main', 40)
    export(p, "SM_Manzanas_Callejones_Galeana.fbx", ARCH_DIR)


# ------------------------------------------------------------------------------
# 4. Calle Hospital y Floreria El Jazmin
# ------------------------------------------------------------------------------
def make_manzanas_hospital():
    reset()
    m = mats()
    p = MeshBuilder()
    street_row(p, m, 'x', 22.5, 18.0, 27.0, -1, 'main', 51)
    street_row(p, m, 'x', 22.5, 32.0, 95.0, -1, 'main', 52)
    street_row(p, m, 'x', 7.5, 18.0, 95.0, +1, 'main', 53)
    street_row(p, m, 'z', 18.0, 7.5, 22.5, +1, 'main', 54, depth=2.5)

    row = Row(p, 'z', 95.0, -1)
    zc = 15.0
    row.fbox(zc, 4.75, -4.0, 15.0, 9.5, 8.0, m['blanco'])
    row.fbox(zc, 0.3, 0.07, 15.0, 0.6, 0.14, m['cantera'])
    row.fbox(zc - 2.0, 1.7, 0.05, 6.0, 2.8, 0.10, m['cristal'])
    row.fbox(zc - 2.0, 1.7, 0.03, 6.4, 3.1, 0.06, m['cantera'])
    row.fbox(zc + 3.6, 1.5, 0.05, 1.8, 3.0, 0.10, m['madera'])
    row.fbox(zc + 3.6, 1.6, 0.03, 2.2, 3.2, 0.06, m['cantera'])
    row.fbox(zc, 3.6, 0.9, 11.0, 0.08, 1.8, m['toldo'])
    row.fbox(zc, 4.4, 0.08, 9.0, 0.9, 0.16, m['floreria'])
    for dz in [-4.5, -3.0, -1.5, 0.0, 1.5]:
        row.fbox(zc + dz, 0.3, 0.7, 1.1, 0.6, 0.7, m['floreria'])
        row.fbox(zc + dz, 0.75, 0.7, 1.0, 0.4, 0.6, m['amarillo'])
    for dz in [-4.0, 0.0, 4.0]:
        row.fbox(zc + dz, 6.3, 0.06, 1.4, 2.0, 0.12, m['cantera'])
        row.fbox(zc + dz, 6.3, 0.07, 1.1, 1.7, 0.14, m['vidrio'])
        row.fbox(zc + dz, 5.3, 0.62, 1.8, 0.9, 0.04, m['herreria'])
        row.fbox(zc + dz, 4.85, 0.32, 1.8, 0.16, 0.64, m['cantera'])
    row.fbox(zc, 9.25, 0.2, 15.0, 0.5, 0.4, m['cantera'])
    export(p, "SM_Manzanas_Hospital_Floreria.fbx", ARCH_DIR)


# ------------------------------------------------------------------------------
# 5. Props (origen en el piso, frente hacia -Z)
# ------------------------------------------------------------------------------
def make_farola():
    reset()
    m = mats()
    p = MeshBuilder()
    box(p, (0.5, 0.7, 0.5), (0.0, 0.35, 0.0), m['herreria'])
    box(p, (0.16, 2.4, 0.16), (0.0, 1.9, 0.0), m['herreria'])
    box(p, (0.45, 0.10, 0.12), (0.22, 3.1, 0.0), m['herreria'])
    box(p, (0.32, 0.40, 0.32), (0.40, 3.35, 0.0), m['foco'])
    box(p, (0.38, 0.18, 0.38), (0.40, 3.64, 0.0), m['herreria'])
    export(p, "SM_Farola_Colonial_GDL.fbx", PROPS_DIR)


def person(p, m, rng, x, z, police=False, sign=False):
    s = rng.uniform(0.92, 1.08)
    if police:
        cloth, legs = m['policia'], m['policia']
    else:
        cloth = m[rng.choice(['ropa_1', 'ropa_2', 'ropa_3', 'ropa_4', 'ropa_5'])]
        legs = m[rng.choice(['pantalon', 'ropa_5', 'ropa_1'])]
    skin = m[rng.choice(['piel_1', 'piel_1', 'piel_2'])]
    box(p, (0.17 * s, 0.85 * s, 0.2 * s), (x - 0.11 * s, 0.425 * s, z), legs)
    box(p, (0.17 * s, 0.85 * s, 0.2 * s), (x + 0.11 * s, 0.425 * s, z), legs)
    box(p, (0.48 * s, 0.68 * s, 0.26 * s), (x, 1.19 * s, z), cloth)
    box(p, (0.12 * s, 0.62 * s, 0.14 * s), (x - 0.31 * s, 1.18 * s, z), cloth)
    box(p, (0.12 * s, 0.62 * s, 0.14 * s), (x + 0.31 * s, 1.18 * s, z), cloth)
    box(p, (0.22 * s, 0.26 * s, 0.24 * s), (x, 1.68 * s, z), skin)
    if police:
        box(p, (0.26 * s, 0.14 * s, 0.28 * s), (x, 1.84 * s, z), m['casco'])
        box(p, (0.62, 1.15, 0.05), (x, 0.95, z - 0.30), m['escudo'])
    elif sign:
        box(p, (0.04, 1.0, 0.04), (x + 0.3, 1.9, z - 0.12), m['madera'])
        box(p, (rng.uniform(0.7, 1.2), 0.55, 0.03), (x + 0.3, 2.35, z - 0.15), m['cartulina'])


def make_multitud():
    reset()
    m = mats()
    p = MeshBuilder()
    rng = random.Random(7)
    for _ in range(32):
        x = rng.uniform(-6.0, 6.0)
        z = rng.uniform(-3.5, 3.5)
        person(p, m, rng, x, z, police=False, sign=rng.random() < 0.35)
    box(p, (0.05, 1.9, 0.05), (-1.6, 0.95, -3.9), m['madera'])
    box(p, (0.05, 1.9, 0.05), (1.6, 0.95, -3.9), m['madera'])
    box(p, (3.3, 0.9, 0.03), (0.0, 1.5, -3.92), m['cartulina'])
    export(p, "SM_Multitud_Protesta.fbx", PROPS_DIR)


def make_linea_policia():
    reset()
    m = mats()
    p = MeshBuilder()
    rng = random.Random(9)
    n = 9
    for i in range(n):
        x = -5.0 + i * (10.0 / (n - 1))
        person(p, m, rng, x, rng.uniform(-0.1, 0.1), police=True)
    export(p, "SM_Linea_Policia.fbx", PROPS_DIR)


def make_patrulla():
    reset()
    m = mats()
    p = MeshBuilder()
    box(p, (1.85, 0.75, 4.6), (0.0, 0.70, 0.0), m['patrulla_b'])
    box(p, (1.86, 0.25, 4.62), (0.0, 0.55, 0.0), m['patrulla_n'])
    box(p, (1.65, 0.6, 2.3), (0.0, 1.37, 0.25), m['patrulla_n'])
    box(p, (0.6, 0.12, 0.3), (-0.32, 1.73, 0.0), m['torreta_r'])
    box(p, (0.6, 0.12, 0.3), (0.32, 1.73, 0.0), m['torreta_a'])
    for x in [-0.88, 0.88]:
        for z in [-1.45, 1.45]:
            box(p, (0.25, 0.64, 0.64), (x, 0.32, z), m['llanta'])
    export(p, "SM_Patrulla.fbx", PROPS_DIR)


def make_contenedor():
    reset()
    m = mats()
    p = MeshBuilder()
    box(p, (1.8, 1.1, 1.1), (0.0, 0.65, 0.0), m['contenedor'])
    box(p, (1.85, 0.08, 1.15), (0.0, 1.24, 0.0), m['contenedor'])
    for x in [-0.75, 0.75]:
        for z in [-0.45, 0.45]:
            box(p, (0.12, 0.1, 0.12), (x, 0.05, z), m['llanta'])
    rng = random.Random(3)
    for _ in range(4):
        box(p, (0.5, 0.45, 0.5), (rng.uniform(-1.2, 1.2), 0.225, rng.uniform(-1.1, -0.7)), m['bolsa'])
    export(p, "SM_Contenedor_Basura.fbx", PROPS_DIR)


def make_cono():
    reset()
    m = mats()
    p = MeshBuilder()
    box(p, (0.42, 0.04, 0.42), (0.0, 0.02, 0.0), m['herreria'])
    box(p, (0.30, 0.25, 0.30), (0.0, 0.165, 0.0), m['cono'])
    box(p, (0.22, 0.22, 0.22), (0.0, 0.40, 0.0), m['linea_blanca'])
    box(p, (0.14, 0.22, 0.14), (0.0, 0.62, 0.0), m['cono'])
    export(p, "SM_Cono_Obra.fbx", PROPS_DIR)


def make_cinta():
    reset()
    m = mats()
    p = MeshBuilder()
    for x in [-2.5, 2.5]:
        box(p, (0.08, 1.0, 0.08), (x, 0.5, 0.0), m['poste'])
        box(p, (0.3, 0.05, 0.3), (x, 0.025, 0.0), m['poste'])
    box(p, (5.0, 0.08, 0.01), (0.0, 0.92, 0.0), m['cinta'])
    box(p, (5.0, 0.08, 0.01), (0.0, 0.55, 0.0), m['cinta'])
    export(p, "SM_Cinta_Precaucion.fbx", PROPS_DIR)


def make_valla():
    reset()
    m = mats()
    p = MeshBuilder()
    metal = m['metal_valla']

    tube_r = 0.024
    frame_w = 2.40
    h_top = 1.20
    h_bot = 0.20

    # Marco tubular superior, inferior y laterales
    p.add_cylinder(tube_r, frame_w, (0.0, h_top, 0.0), metal, axis='X')
    p.add_cylinder(tube_r, frame_w, (0.0, h_bot, 0.0), metal, axis='X')
    vert_h = h_top - h_bot
    vert_y = (h_top + h_bot) * 0.5
    p.add_cylinder(tube_r, vert_h, (-frame_w * 0.5, vert_y, 0.0), metal, axis='Y')
    p.add_cylinder(tube_r, vert_h, (+frame_w * 0.5, vert_y, 0.0), metal, axis='Y')

    # Barrotes interiores
    num_bars = 13
    bar_h = vert_h - tube_r * 2.0
    spacing = (frame_w - 0.30) / (num_bars - 1)
    start_x = -(frame_w - 0.30) * 0.5
    for i in range(num_bars):
        p.add_cylinder(0.010, bar_h, (start_x + i * spacing, vert_y, 0.0), metal, axis='Y', segments=8)

    # Patas de apoyo estables apoyadas en Y = 0
    foot_r = 0.020
    angle = math.radians(45)
    for sx in [-0.85, 0.85]:
        p.add_cylinder(foot_r, 0.60, (sx, foot_r, 0.0), metal, axis='Z', segments=10)
        p.add_cylinder(tube_r, h_bot, (sx, h_bot * 0.5, 0.0), metal, axis='Y', segments=10)
        p.add_cylinder(0.012, 0.30, (sx, 0.10, -0.12), metal, axis='Y', rot_x_rad=-angle, segments=8)
        p.add_cylinder(0.012, 0.30, (sx, 0.10, +0.12), metal, axis='Y', rot_x_rad=+angle, segments=8)

    # Dos placas frontales mirando hacia -Z (hacia el jugador en Av. Juarez)
    p.add_box((0.86, 0.44, 0.02), (-0.52, 0.72, -0.04), m['placa_cerrada'], uv_plate=True)
    p.add_box((0.86, 0.44, 0.02), (+0.52, 0.72, -0.04), m['placa_desvio'], uv_plate=True)

    export(p, "SM_Valla_Bloqueo.fbx", PROPS_DIR)


if __name__ == "__main__":
    make_piso()
    make_manzanas_juarez()
    make_manzanas_callejones()
    make_manzanas_hospital()
    make_farola()
    make_multitud()
    make_linea_policia()
    make_patrulla()
    make_contenedor()
    make_cono()
    make_cinta()
    make_valla()
    with open(BOUNDS_JSON, "w", encoding="utf-8") as fh:
        json.dump({"items": BOUNDS}, fh, indent=2)
    print(f"[OK] {len(BOUNDS)} modelos y limites escritos en {BOUNDS_JSON}")
