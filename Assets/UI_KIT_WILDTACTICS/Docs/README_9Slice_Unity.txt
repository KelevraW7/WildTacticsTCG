# UI_KIT_WILDTACTICS_9SLICE

Pack base para la pantalla JUGAR / menú principal de WildTactics.

Todos los sprites con sufijo `_9S` están preparados conceptualmente para 9-slice:
- Esquinas decorativas dentro del área de borde.
- Centro limpio y estirable.
- Bordes simples para escalar sin deformar las esquinas.

## Recomendación de importación en Unity

Sprite Mode: Single
Mesh Type: Full Rect
Pixels Per Unit: 100
Sprite Editor > Border:
- Botones 512x160: L/R 48, T/B 48
- Tabs 420x150: L/R 42, T/B 42
- Paneles medianos: L/R 58-70, T/B 58-70
- ModeCard 520x760: L/R 70, T/B 70
- TopBar 1400x180: L/R 60, T/B 60

Image Type en UI:
- Sliced para botones, paneles, tabs, marcos y tarjetas.
- Simple para iconos y FX.

## Uso sugerido

Buttons/
- WT_Button_Primary_Normal_9S
- WT_Button_Primary_Hover_9S
- WT_Button_Primary_Disabled_9S
- WT_Button_Secondary_Normal_9S
- WT_Button_Secondary_Hover_9S
- WT_TopTab_Normal_9S
- WT_TopTab_Active_9S

Panels/
- WT_TopBar_Backplate_9S
- WT_ProfilePanel_9S
- WT_ModeCard_Normal_9S
- WT_ModeCard_Hover_9S
- WT_ModeCard_Selected_9S
- WT_ModeCard_Locked_9S
- WT_DifficultyModal_9S
- WT_DifficultySlot_Normal_9S
- WT_DifficultySlot_Hover_9S

Icons/
- Settings, Lock, WC, Profile, Close, Arrow

FX/
- Glows reutilizables para hover/active.
