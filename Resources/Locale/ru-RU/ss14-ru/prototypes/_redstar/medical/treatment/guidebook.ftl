entity-condition-guidebook-on-fire = цель
    { $invert ->
        [true] не горит
        *[false] горит
    }
entity-effect-guidebook-heal-bone-damage = Восстанавливает повреждения костей на {$amount}.
entity-condition-guidebook-inventory-nested = любой надетый предмет соответствует условию: {$condition}
entity-condition-guidebook-pressure-protection = имеет защиту от давления
reagent-effect-condition-guidebook-stamina-damage-threshold =
    { $max ->
        [2147483648] цель имеет не менее {NATURALFIXED($min, 2)} единиц урона выносливости
        *[other] { $min ->
                    [0] цель имеет не более {NATURALFIXED($max, 2)} единиц урона выносливости
                    *[other] цель имеет от {NATURALFIXED($min, 2)} до {NATURALFIXED($max, 2)} единиц урона выносливости
                 }
    }
reagent-effect-condition-guidebook-typed-damage-threshold = цель превышает порог урона: { $changes }
reagent-effect-condition-guidebook-unique-bloodstream-chem-threshold =
    { $max ->
        [2147483648] { $min ->
                        [1] в крови присутствует более {$min} реагента
                        *[other] в крови присутствует более {$min} реагентов
                     }
        [1] { $min ->
               [0] в крови присутствует не более {$max} реагента
               *[other] в крови присутствует от {$min} до {$max} реагентов
            }
        *[other] { $min ->
                    [-1] в крови присутствует не более {$max} реагентов
                    *[other] в крови присутствует от {$min} до {$max} реагентов
                 }
    }
reagent-effect-guidebook-deal-stamina-damage =
    { $chance ->
        [1] { $deltasign ->
                [1] Наносит
                *[-1] Восстанавливает
            }
        *[other]
            { $deltasign ->
                [1] наносит
                *[-1] восстанавливает
            }
    } {$amount} ед. урона выносливости { $immediate ->
                    [true] мгновенно
                    *[false] со временем
                  }
