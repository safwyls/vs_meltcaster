# Meltcaster Recipes

This directory contains the recipes for the Meltcaster project. The mod will search all subdirectories for .json files and attempt to parse them for meltcaster recipes adding them to the stored list.

## Example recipe file format
``` json
[
    {
        input: { code: "game:metal-scraps", type: "block", stacksize: 1 },
        meltcasttemp: 1200,
        meltcasttime: 60,
        output: [
        {
            code: "meltcaster:g-metal-bits",
            stacksize: 1,
            chance: 1,
            group: [
            { code: "game:metalbit-copper", type: "item", stacksize: 1, chance: 1 },
            { code: "game:metalbit-tinbronze", type: "item", stacksize: 1, chance: 0.7 },
            { code: "game:metalbit-bismuthbronze", type: "item", stacksize: 1, chance: 0.6 },
            { code: "game:metalbit-blackbronze", type: "item", stacksize: 1, chance: 0.5 },
            { code: "game:metalbit-iron", type: "item", stacksize: 1, chance: 0.5 },
            { code: "game:metalbit-steel", type: "item", stacksize: 1, chance: 0.4 },
            { code: "game:metalbit-nickel", type: "item", stacksize: 1, chance: 0.3 },
            { code: "game:metalbit-lead", type: "item", stacksize: 1, chance: 0.3 },
            { code: "game:metalbit-zinc", type: "item", stacksize: 1, chance: 0.3 },
            { code: "game:metalbit-bismuth", type: "item", stacksize: 1, chance: 0.3 },
            { code: "game:metalbit-molybdochalkos", type: "item", stacksize: 1, chance: 0.3 },
            { code: "game:metalbit-cupronickel", type: "item", stacksize: 1, chance: 0.2 },
            { code: "game:metalbit-brass", type: "item", stacksize: 1, chance: 0.2 },
            { code: "game:metalbit-chromium", type: "item", stacksize: 1, chance: 0.2 },
            { code: "game:metalbit-silver", type: "item", stacksize: 1, chance: 0.2 },
            { code: "game:metalbit-leadsolder", type: "item", stacksize: 1, chance: 0.2 },
            { code: "game:metalbit-silversolder", type: "item", stacksize: 1, chance: 0.1 },
            { code: "game:metalbit-meteoriciron", type: "item", stacksize: 1, chance: 0.1 },
            { code: "game:metalbit-gold", type: "item", stacksize: 1, chance: 0.1 },
            { code: "game:metalbit-electrum", type: "item", stacksize: 1, chance: 0.05 },
            { code: "game:metalbit-titanium", type: "item", stacksize: 1, chance: 0.05 },
            ],
        },
        { code: "game:gear-rusty", type: "item", stacksize: 1, chance: 0.05 },
        { code: "game:gear-temporal", type: "item", stacksize: 1, chance: 0.01 },
        ],
    },
]
```