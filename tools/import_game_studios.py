import os
import urllib.request

BASE_RAW = "https://raw.githubusercontent.com/Donchitos/Claude-Code-Game-Studios/main/"

TARGET_FILES = {
    # 1. Departamento: Level Design
    ".claude/agents/level-designer.md": ".agents/game-development/ccgs/level-designer.md",
    ".claude/agents/world-builder.md": ".agents/game-development/ccgs/world-builder.md",
    ".claude/agents/technical-artist.md": ".agents/game-development/ccgs/technical-artist.md",

    # 2. Departamento: Unity Engineering
    ".claude/agents/unity-specialist.md": ".agents/game-development/ccgs/unity-specialist.md",
    ".claude/agents/unity-ui-specialist.md": ".agents/game-development/ccgs/unity-ui-specialist.md",
    ".claude/agents/unity-shader-specialist.md": ".agents/game-development/ccgs/unity-shader-specialist.md",
    ".claude/agents/gameplay-programmer.md": ".agents/game-development/ccgs/gameplay-programmer.md",
    ".claude/agents/lead-programmer.md": ".agents/game-development/ccgs/lead-programmer.md",

    # 3. Departamento: Narrative & Environmental Storytelling
    ".claude/agents/narrative-director.md": ".agents/game-development/ccgs/narrative-director.md",
    ".claude/agents/writer.md": ".agents/game-development/ccgs/writer.md",

    # 4. Departamento: Audio & Tension
    ".claude/agents/audio-director.md": ".agents/game-development/ccgs/audio-director.md",
    ".claude/agents/sound-designer.md": ".agents/game-development/ccgs/sound-designer.md",

    # 5. Departamento: QA, Performance & Physics
    ".claude/agents/qa-lead.md": ".agents/game-development/ccgs/qa-lead.md",
    ".claude/agents/qa-tester.md": ".agents/game-development/ccgs/qa-tester.md",
    ".claude/agents/performance-analyst.md": ".agents/game-development/ccgs/performance-analyst.md",

    # Skills de Coordinación de Estudio
    ".claude/skills/team-level/SKILL.md": ".agents/ccgs-skills/team-level/SKILL.md",
    ".claude/skills/team-audio/SKILL.md": ".agents/ccgs-skills/team-audio/SKILL.md",
    ".claude/skills/team-narrative/SKILL.md": ".agents/ccgs-skills/team-narrative/SKILL.md",
    ".claude/skills/team-ui/SKILL.md": ".agents/ccgs-skills/team-ui/SKILL.md",
    ".claude/skills/team-qa/SKILL.md": ".agents/ccgs-skills/team-qa/SKILL.md",
    ".claude/skills/vertical-slice/SKILL.md": ".agents/ccgs-skills/vertical-slice/SKILL.md",
    ".claude/skills/gate-check/SKILL.md": ".agents/ccgs-skills/gate-check/SKILL.md",
    ".claude/skills/smoke-check/SKILL.md": ".agents/ccgs-skills/smoke-check/SKILL.md",
    ".claude/skills/perf-profile/SKILL.md": ".agents/ccgs-skills/perf-profile/SKILL.md",
    ".claude/skills/balance-check/SKILL.md": ".agents/ccgs-skills/balance-check/SKILL.md",
    ".claude/skills/art-bible/SKILL.md": ".agents/ccgs-skills/art-bible/SKILL.md",

    # Referencias de Arquitectura Unity
    "docs/engine-reference/unity/current-best-practices.md": "docs/studio/unity/current-best-practices.md",
    "docs/engine-reference/unity/modules/input.md": "docs/studio/unity/input.md",
    "docs/engine-reference/unity/modules/physics.md": "docs/studio/unity/physics.md",
    "docs/engine-reference/unity/modules/rendering.md": "docs/studio/unity/rendering.md",
    "docs/engine-reference/unity/modules/audio.md": "docs/studio/unity/audio.md",
    "docs/engine-reference/unity/modules/ui.md": "docs/studio/unity/ui.md",
}

print(f"Descargando e integrando {len(TARGET_FILES)} componentes clave de CCGS...")
count = 0
for src, dst in TARGET_FILES.items():
    url = BASE_RAW + src
    dst_dir = os.path.dirname(dst)
    if dst_dir:
        os.makedirs(dst_dir, exist_ok=True)
    try:
        req = urllib.request.Request(url, headers={'User-Agent': 'RutaSegura/1.0'})
        content = urllib.request.urlopen(req).read().decode('utf-8')
        with open(dst, 'w', encoding='utf-8') as f:
            f.write(content)
        count += 1
        print(f" [OK] {dst}")
    except Exception as e:
        print(f" [ERROR] {src}: {e}")

print(f"\n¡Completado! {count}/{len(TARGET_FILES)} archivos integrados exitosamente.")
