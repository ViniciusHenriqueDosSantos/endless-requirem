# Paleta do campus UNIFESP São José dos Campos, tirada das fotos do tour 360° (pro360.com.br).
# Os nomes dizem de onde veio cada cor. Mude aqui e rode build_all.py para atualizar tudo.
from pix import hexc

C = {
    # --- contorno e sombra geral
    "ink": hexc("1c1a22"),          # contorno dos personagens
    "shadow": hexc("000000", 70),   # sombra no chão
    "void": hexc("17181d"),

    # --- calçada de bloquete vermelho (portão e entrada)
    "brick": hexc("93412f"), "brick_l": hexc("ab5440"), "brick_d": hexc("75332a"),
    "brick_hi": hexc("c06a52"), "brick_grout": hexc("5e2a23"),

    # --- calçada cimentada e meio-fio
    "walk": hexc("b9b6ad"), "walk_l": hexc("c8c5bc"), "walk_d": hexc("a09d95"), "walk_joint": hexc("8d8a83"),
    "curb": hexc("d2d0c8"), "curb_d": hexc("96948d"),

    # --- asfalto da avenida
    "asph": hexc("5c5e62"), "asph_l": hexc("6a6c70"), "asph_d": hexc("4d4f53"), "paint": hexc("e6e4dc"),

    # --- piso sextavado do estacionamento
    "hex": hexc("aeaaa2"), "hex_l": hexc("bfbbb2"), "hex_d": hexc("8b8780"),

    # --- granito claro da praça entre os prédios
    "plaza": hexc("d6cfc0"), "plaza_l": hexc("e3ddd1"), "plaza_d": hexc("bcb4a4"), "plaza_joint": hexc("a9a191"),

    # --- granito branco polido (recepção, cantina, corredores internos)
    "gran": hexc("e3e5e7"), "gran_l": hexc("f3f4f5"), "gran_m": hexc("cdd0d4"), "gran_d": hexc("b2b6bc"),
    "gran_k": hexc("8f949b"), "gran_joint": hexc("d3d6d9"),

    # --- cerâmica bege do RU
    "cer": hexc("e8ddc7"), "cer_l": hexc("f2ead9"), "cer_d": hexc("d9ccb2"), "cer_grout": hexc("c4b699"),

    # --- chapa de aço do elevador
    "steel": hexc("a7adb4"), "steel_l": hexc("c9ced3"), "steel_d": hexc("7c838b"), "steel_k": hexc("5d636b"),

    # --- concreto aparente das fachadas
    "conc": hexc("d1c9b7"), "conc_l": hexc("e0d9c9"), "conc_d": hexc("b3aa96"), "conc_k": hexc("958c79"),
    "conc_stain": hexc("bfb6a2"),
    "glass": hexc("2c3a4a"), "glass_l": hexc("4f6e8e"), "glass_hi": hexc("8fb3cf"), "frame": hexc("8e959d"),

    # --- parede interna branca
    "wall": hexc("eceeee"), "wall_m": hexc("dcdfe1"), "wall_d": hexc("c3c7cb"), "base": hexc("a9aeb4"),
    "cap": hexc("c9ccd1"), "cap_l": hexc("d9dce0"), "cap_d": hexc("9fa4ab"),

    # --- verde (gradil, grama, plantas)
    "fence": hexc("2e7a4c"), "fence_l": hexc("45a06a"), "fence_d": hexc("1f5636"),
    "grass": hexc("5c9f43"), "grass_l": hexc("78b955"), "grass_d": hexc("467f33"), "grass_k": hexc("35652a"),
    "leaf": hexc("3f8c3b"), "leaf_l": hexc("62ad4c"), "leaf_d": hexc("2b662c"), "agave": hexc("7fae8e"),
    "agave_d": hexc("57866a"), "trunk": hexc("8a7656"), "trunk_d": hexc("665639"),

    # --- identidade UNIFESP (letreiro da fachada)
    "uni_blue": hexc("2459a6"), "uni_blue_l": hexc("4f86cf"), "uni_text": hexc("3a3f48"),

    # --- madeira, tecido, móveis
    "wood": hexc("9a5f33"), "wood_l": hexc("b97a46"), "wood_d": hexc("6e4223"),
    "felt": hexc("1f7a45"), "felt_l": hexc("2f9a59"), "felt_d": hexc("165a33"),
    "table": hexc("eef0ec"), "table_d": hexc("c9ccc6"), "table_k": hexc("9ea29c"),
    "black": hexc("2a2b30"), "black_l": hexc("44464d"),
    "inox": hexc("bcc3ca"), "inox_l": hexc("dde2e6"), "inox_d": hexc("8a929b"),

    # --- coleta seletiva (lixeiras da cantina e do RU)
    "bin_red": hexc("c7372f"), "bin_blue": hexc("2c64b8"), "bin_yellow": hexc("e3b62a"), "bin_green": hexc("2f8f46"),

    # --- personagens
    "skin": hexc("c98b62"), "skin_l": hexc("e0a67c"), "skin_d": hexc("a46a46"),
    "hair": hexc("2b211c"), "hair_l": hexc("4a3a30"),
    "hood": hexc("23476e"), "hood_l": hexc("2f6196"), "hood_d": hexc("182f4b"),
    "jeans": hexc("3d4c66"), "jeans_d": hexc("2c374b"),
    "shoe": hexc("eceae4"), "shoe_d": hexc("aaa69c"),
    "pack": hexc("6b3f2a"), "pack_d": hexc("4a2b1d"),
    "lanyard": hexc("2bb673"),
    "ghost": hexc("e9f1f6"), "ghost_m": hexc("bccbd8"), "ghost_d": hexc("8698ad"),
    "cap_grad": hexc("23232b"), "tassel": hexc("f2c230"),
}
