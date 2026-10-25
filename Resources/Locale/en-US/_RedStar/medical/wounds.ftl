alerts-broken-bones-name = Broken Bones
alerts-broken-bones-desc = You have one or more broken bones. Get medical attention as soon as possible.

popup-trauma-BoneDamage-Damaged = You feel some pain in your {$part}.
popup-trauma-BoneDamage-Cracked = You feel a sharp pain in your {$part}!
popup-trauma-BoneDamage-Broken = You hear a loud crack in your {$part}!!
self-inspect-trauma-BoneDamage = hurts inside
self-inspect-trauma-BoneDamage-Large = feels dislocated
inspect-trauma-BoneDamage = looks dislocated

popup-trauma-OrganDamage-Damaged = Your {$part} feels very wrong...
popup-trauma-OrganDamage-Destroyed = You feel a very sharp pain in your {$part}!
self-inspect-trauma-OrganDamage = feels weird every time you breathe.

inspect-wound-minor = injured
inspect-wound-moderate = hurt
inspect-wound-severe = badly damaged
inspect-wound-critical = mangled
inspect-wound-loss = completely mangled

inspect-wound-Bleeding-minor = is bleeding a little
inspect-wound-Bleeding-moderate = is bleeding
inspect-wound-Bleeding-severe = is bleeding profusely

inspect-wound-Blunt-minor = bruised
inspect-wound-Blunt-moderate = battered
inspect-wound-Blunt-severe = heavily bruised
inspect-wound-Slash-minor = cut
inspect-wound-Slash-moderate = lacerated
inspect-wound-Slash-severe = almost severed.
inspect-wound-Pierce-minor = stabbed
inspect-wound-Pierce-moderate = punctured
inspect-wound-Pierce-severe = severely impaled
inspect-wound-Ballistic-minor = shot
inspect-wound-Ballistic-moderate = blasted
inspect-wound-Ballistic-severe = littered with bullet holes

inspect-wound-Burn-minor = burned
inspect-wound-Burn-moderate = blistering
inspect-wound-Burn-severe = charred
inspect-wound-Cold-minor = cold
inspect-wound-Cold-moderate = frozen
inspect-wound-Cold-severe = frostbitten
inspect-wound-Shock-minor = arc burned
inspect-wound-Shock-moderate = zapped
inspect-wound-Shock-severe = covered in electrical burns
inspect-wound-Caustic-minor = severely burned
inspect-wound-Caustic-moderate = melted
inspect-wound-Caustic-severe = necrotic

inspect-wound-Holy-minor = glowing
inspect-wound-Holy-moderate = melted
inspect-wound-Holy-severe = completely covered in acid burns

inspect-wound-weeping-avulsion-minor = torn open
inspect-wound-weeping-avulsion-moderate = completely torn open
inspect-wound-weeping-avulsion-severe = completely torn open, along with significant loss of tissue

armor-coverage-value = - This armor protects your [color=yellow]{ $type }[/color].
armor-coverage-type-arm = arms
armor-coverage-type-hand = hands
armor-coverage-type-foot = feet
armor-coverage-type-leg = legs
armor-coverage-type-torso = torso
armor-coverage-type-wings = wings
armor-coverage-type-head = head
armor-coverage-type-tail = tail
armor-coverage-type-other = other parts

-protection = damage { $protect ->
    [true] reduced
   *[false] [color=red]increased[/color]
    } by [color=lightblue]{TOSTRING($value, "F1")}%[/color].
armor-coefficient-value-trauma = - [color=yellow]{$type}[/color] { -protection(protect: $protect, value: $value) }
armor-damage-type-ballistic = Ballistic
armor-damage-type-ion = Ion
entity-category-name-wounds = Wounds
entity-category-name-traumas = Traumas
damage-type-ballistic = Ballistic
damage-type-ion = Ion
damage-group-electronic = Electronic


armor-point-blank-examine = Protection is [color=red]multiplied by { $penalty }x[/color] within a { $range }m range on a precise hit
