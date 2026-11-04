alerts-broken-bones-name = Переломы
alerts-broken-bones-desc = У вас сломана одна или несколько костей. Как можно скорее обратитесь за медицинской помощью.

popup-trauma-BoneDamage-Damaged = Вы чувствуете слабую боль в области: { $part }.
popup-trauma-BoneDamage-Cracked = Вы чувствуете резкую боль в области: { $part }!
popup-trauma-BoneDamage-Broken = Вы слышите громкий хруст в области: { $part }!!
self-inspect-trauma-BoneDamage = болит изнутри
self-inspect-trauma-BoneDamage-Large = ощущается вывих
inspect-trauma-BoneDamage = виден вывих

popup-trauma-OrganDamage-Damaged = Вы чувствуете, что с { $part } что-то не так...
popup-trauma-OrganDamage-Destroyed = Вы чувствуете очень резкую боль в области: { $part }!
self-inspect-trauma-OrganDamage = неприятные ощущения при каждом вдохе.

inspect-wound-minor = небольшие повреждения
inspect-wound-moderate = повреждения
inspect-wound-severe = тяжёлые повреждения
inspect-wound-critical = сильное увечье
inspect-wound-loss = полное разрушение тканей

inspect-wound-Bleeding-minor = слегка кровоточит
inspect-wound-Bleeding-moderate = кровоточит
inspect-wound-Bleeding-severe = сильно кровоточит

inspect-wound-Blunt-minor = синяки
inspect-wound-Blunt-moderate = ушибы
inspect-wound-Blunt-severe = сильные ушибы
inspect-wound-Slash-minor = порезы
inspect-wound-Slash-moderate = рваные раны
inspect-wound-Slash-severe = почти отсечено.
inspect-wound-Pierce-minor = колотые раны
inspect-wound-Pierce-moderate = глубокие колотые раны
inspect-wound-Pierce-severe = сквозные колотые раны
inspect-wound-Ballistic-minor = огнестрельные ранения
inspect-wound-Ballistic-moderate = тяжёлые огнестрельные ранения
inspect-wound-Ballistic-severe = множество пулевых отверстий

inspect-wound-Burn-minor = ожоги
inspect-wound-Burn-moderate = ожоговые волдыри
inspect-wound-Burn-severe = обугленные ткани
inspect-wound-Cold-minor = переохлаждение
inspect-wound-Cold-moderate = замёрзшие ткани
inspect-wound-Cold-severe = обморожение
inspect-wound-Shock-minor = ожоги от электрической дуги
inspect-wound-Shock-moderate = следы удара током
inspect-wound-Shock-severe = обширные электрические ожоги
inspect-wound-Caustic-minor = сильные химические ожоги
inspect-wound-Caustic-moderate = разъеденные ткани
inspect-wound-Caustic-severe = некроз

inspect-wound-Holy-minor = свечение
inspect-wound-Holy-moderate = расплавленные ткани
inspect-wound-Holy-severe = обширные кислотные ожоги

inspect-wound-weeping-avulsion-minor = оторванные ткани
inspect-wound-weeping-avulsion-moderate = обширный отрыв тканей
inspect-wound-weeping-avulsion-severe = обширный отрыв со значительной потерей тканей

armor-coverage-value = - Эта броня защищает: [color=yellow]{ $type }[/color].
armor-coverage-type-arm = руки
armor-coverage-type-hand = кисти
armor-coverage-type-foot = стопы
armor-coverage-type-leg = ноги
armor-coverage-type-torso = туловище
armor-coverage-type-wings = крылья
armor-coverage-type-head = голову
armor-coverage-type-tail = хвост
armor-coverage-type-other = другие части тела

-protection = урон { $protect ->
    [true] снижен
   *[false] [color=red]увеличен[/color]
    } на [color=lightblue]{ TOSTRING($value, "F1") }%[/color].
armor-coefficient-value-trauma = - [color=yellow]{ $type }[/color]: { -protection(protect: $protect, value: $value) }
armor-damage-type-ballistic = Баллистический
armor-damage-type-ion = Ионный
entity-category-name-wounds = Раны
entity-category-name-traumas = Травмы
damage-type-ballistic = Баллистический
damage-type-ion = Ионный
damage-group-electronic = Электронный

armor-point-blank-examine = Защита [color=red]умножается на { $penalty }[/color] при точном попадании с расстояния до { $range } м
