guide-entry-beekeeping = Пчеловодство
guide-entry-beekeeping-analyzer = Анализатор пасеки
guide-entry-beekeeping-queen = Матка и признаки
guide-entry-beekeeping-wild = Рои и дикие ульи

## Улей

beekeeping-slot-queen = Матка
beekeeping-slot-frame-1 = Рамка 1
beekeeping-slot-frame-2 = Рамка 2
beekeeping-slot-frame-3 = Рамка 3
beekeeping-slot-frame-4 = Рамка 4

beekeeping-hive-examine-queen = В улье есть [color=yellow]матка[/color].
beekeeping-hive-examine-no-queen = В улье [color=red]нет матки[/color].
beekeeping-hive-examine-frames = Рамок: { $count } из { $max }.
beekeeping-hive-examine-bees = Пчёл в улье: { $bees } из { $max }{ $outside ->
    [0] {""}
   *[other] , снаружи ещё { $outside }
}.
beekeeping-hive-examine-nectar = Запас нектара: { $level ->
    [Empty] пусто
    [Low] мало
    [Medium] средне
   *[High] много
}.
beekeeping-hive-examine-cramped = [color=yellow]В улье тесно: пчёлам не хватает рамок.[/color]
beekeeping-hive-examine-leaving = [color=red]Пчёлы покидают улей: не хватает рамок.[/color]
beekeeping-hive-examine-dying = [color=red]Колония без матки вымирает.[/color]
beekeeping-hive-examine-scout-outside = Разведчица вне улья.
beekeeping-hive-examine-scout-lost = Улей потерял разведчицу. Новая появится через { $minutes } мин { $seconds } с.
beekeeping-hive-examine-scout-lost-grow = Улей потерял разведчицу. Новая появится, когда колония подрастёт.
beekeeping-hive-examine-vapor = [color=lightblue]Водяной пар успокаивает пчёл.[/color]
beekeeping-hive-examine-paused = [color=orange]Пчёлы затихли: { $reason ->
    [Vacuum] здесь нечем дышать
    [HighPressure] слишком высокое давление
    [Cold] слишком холодно
    [Heat] слишком жарко
   *[Fire] пожар
}.[/color]

beekeeping-verb-category-frames = Изъять
beekeeping-verb-category-bees = Поймать

## Рамки

beekeeping-frame-examine = { $state ->
    [None] Пустая вощина
    [Honey] Соты заполняются мёдом
   *[Wax] Соты запечатаны, пора срезать
}. Сот: { $combs } из { $max }.
beekeeping-frame-cut-empty = На рамке нечего срезать.
beekeeping-frame-cut-caps = Вы срезаете восковые крышечки: воск ({ $wax }). Теперь можно срезать соты.
beekeeping-frame-cut-combs = Вы срезаете соты ({ $combs }).
beekeeping-frame-pry-in-hand = Положите рамку на пол, чтобы её разобрать.

## Пчёлы в руках

beekeeping-take-too-many = Снаружи и так слишком много пчёл этого улья.
beekeeping-forager-caught = Вы поймали заблудившуюся пчелу. Отнесите её в улей.
beekeeping-take-scout-flying = Разведчица сейчас в полёте, подождите, пока она вернётся.
beekeeping-take-no-scout = В улье нет разведчицы.
beekeeping-take-no-workers = В улье нет свободных рабочих пчёл.
beekeeping-take-scout-done = Вы осторожно достаёте разведчицу.
beekeeping-take-pollinator-done = Вы осторожно достаёте рабочую пчелу.
beekeeping-return-done = Пчела возвращается в улей.
beekeeping-return-wrong-hive = Это не её улей.
beekeeping-return-crowded = В улье слишком тесно: пчела не может вернуться, пока не вернут рамки.

beekeeping-bee-examine-scout = Разведчица. Запомнено лотков: { $count } из { $max }.
beekeeping-bee-examine-pollinator = Опылительница. { $charges ->
    [0] Пыльцы не осталось.
    [one] Пыльцы хватит на { $charges } опыление.
    [few] Пыльцы хватит на { $charges } опыления.
   *[other] Пыльцы хватит на { $charges } опылений.
}
beekeeping-bee-examine-dying = [color=red]Пчела скоро погибнет.[/color]

beekeeping-scout-no-grid = Разведчица не запомнит лоток, который не стоит на полу корабля или станции.
beekeeping-scout-known = Разведчица уже помнит этот лоток.
beekeeping-scout-remembered = Разведчица запоминает лоток ({ $count } из { $max }).

beekeeping-pollinate-no-charges = У пчелы не осталось пыльцы.
beekeeping-pollinate-no-plant = Здесь нечего опылять.
beekeeping-pollinate-dead = Растение погибло.
beekeeping-pollinate-self-harvest = Это растение собирается само, опылять его бесполезно.
beekeeping-pollinate-ripe = Растение уже созрело. Сначала соберите урожай.
beekeeping-pollinate-already = Растение уже опылено до следующего сбора.
beekeeping-pollinate-bonus = Пчела опыляет растение. Следующий урожай будет двойным!
beekeeping-pollinate-no-bonus = Пчела опыляет растение, но двойного урожая не будет.
beekeeping-plant-examine-pollinated = Растение [color=yellow]опылено[/color] до следующего сбора.
beekeeping-plant-examine-pollinated-bonus = Растение [color=yellow]опылено[/color]: следующий урожай будет двойным.

## Признаки

bee-trait-pest-control = Защита растений
bee-trait-pest-control-desc = Опыление снижает вредителей и сорняки в лотке. Врождённый признак, слот не занимает.
bee-trait-sting = Жало
bee-trait-sting-desc = Реагенты, введённые в матку, пчёлы несут в жале и понемногу добавляют в соты.
bee-trait-aggressive = Агрессия
bee-trait-aggressive-desc = Пчёлы злые.
bee-trait-productive = Продуктивность
bee-trait-productive-desc = Соты строятся быстрее, но каждая стоит больше нектара.
bee-trait-efficient-foraging = Эффективный сбор
bee-trait-efficient-foraging-desc = Каждый визит приносит больше нектара.
bee-trait-improved-pollination = Улучшенное опыление
bee-trait-improved-pollination-desc = Опыление чаще удваивает урожай, но у опылительницы на один заряд меньше.

bee-trait-keen-scout = Зоркая разведчица
bee-trait-keen-scout-desc = За вылет разведчица приносит до трёх лотков: тот, где садилась, и соседние.
bee-trait-fertile = Плодовитость
bee-trait-fertile-desc = Матка делает пчёл чаще, а новые пчёлы стоят меньше нектара: колония растёт быстрее.
bee-trait-hardy = Живучесть
bee-trait-hardy-desc = Пчёлы, взятые из улья, живут в полтора раза дольше.
bee-trait-mutagenic-pollination = Мутагенное опыление
bee-trait-mutagenic-pollination-desc = Каждое опыление действует на растение как 1 ед. нестабильного мутагена в лотке.
bee-trait-emergency-queen = Аварийная матка
bee-trait-emergency-queen-desc = Если матка пропала, колония сама выводит новую, обычную матку.
bee-trait-lazy = Лень
bee-trait-lazy-desc = Рабочие строят соты медленнее.
bee-trait-gluttonous = Прожорливость
bee-trait-gluttonous-desc = Каждая сота съедает больше нектара.
bee-trait-frail = Хрупкость
bee-trait-frail-desc = Пчёлы, взятые из улья, погибают быстрее.
bee-trait-clumsy = Неуклюжесть
bee-trait-clumsy-desc = Опыление вдвое реже удваивает урожай.
bee-trait-sluggish = Медлительность
bee-trait-sluggish-desc = Каждый визит приносит меньше нектара.
beekeeping-hive-emergency-queen = Колония вывела новую матку!

beekeeping-traits-none = нет
beekeeping-queen-examine-traits = Признаки: { $traits }.
beekeeping-queen-examine-slots = Слоты признаков: занято { $used } из { $slots }.
beekeeping-queen-examine-payload = Реагенты в матке: { $amount } ед. из { $cap }.
beekeeping-bee-examine-traits = Признаки колонии: { $traits }.
beekeeping-queen-no-sting = Матке нечем это нести: у неё нет жала.
beekeeping-queen-payload-lost = Матка потеряла жало, её реагенты пропали.
beekeeping-queen-trait-gained = Матка получила признак: { $trait }.
beekeeping-queen-trait-lost = Матка потеряла признак: { $trait }.
beekeeping-queen-trait-replaced = Матка получила признак { $trait } вместо признака { $old }.
beekeeping-queen-trait-nothing = Мутаген не подействовал: у матки нет места для признаков.

cmd-beekeeping_clear-desc = Убирает со своей карты злых пчёл, злых маток, рои, заблудившихся пчёл и дикие ульи. С "all" со всех карт.
cmd-beekeeping_clear-help = Использование: beekeeping_clear [all]
cmd-beekeeping_clear-no-map = Нет своей карты: зайдите в тело или укажите "all".
cmd-beekeeping_clear-done = Убрано: { $count }.
beekeeping-queen-hive-limit = Матка не приживается: в секторе слишком много ульев.
beekeeping-bee-bite = Пчела кусает вас за руку!
beekeeping-bee-sting = Пчела жалит вас в руку!

beekeeping-hive-examine-egg = В улье лежит [color=yellow]яйцо матки[/color]. Соберите маточное молочко в ёмкость.
beekeeping-hive-examine-egg-dying = Яйцо матки скоро погибнет.
beekeeping-verb-collect-jelly = Собрать маточное молочко
beekeeping-jelly-no-room = В ёмкости нет места.
beekeeping-jelly-collected = Вы собрали маточное молочко.
beekeeping-queen-slot-added = У матки стало больше места для признаков: слотов { $slots }.
beekeeping-queen-slots-max = У матки уже столько слотов, сколько возможно.

beekeeping-hive-crowd-swarm = Пчёлам тесно: часть колонии улетает роем!
beekeeping-hive-swarm = Старая матка улетает из улья с роем! В улье осталась молодая матка.
## Apiary analyzer

apiary-analyzer-section-traits = Признаки
apiary-analyzer-section-colony-traits = Признаки колонии
apiary-analyzer-section-stats = Характеристики
apiary-analyzer-section-colony = Колония
apiary-analyzer-section-frames = Рамки ({ $count })
apiary-analyzer-section-trays = Известные лотки ({ $count })
apiary-analyzer-section-queen = Матка
apiary-analyzer-section-scout = Разведчица
apiary-analyzer-section-pollinator = Опылительница
apiary-analyzer-no-queen = Матки в улье нет.
apiary-analyzer-stat-comb-rate = Скорость сот
apiary-analyzer-stat-comb-cost = Нектар на соту
apiary-analyzer-stat-nectar = Нектар за визит
apiary-analyzer-stat-pollination = Шанс удвоить урожай
apiary-analyzer-stat-charges = Опылений у пчелы
apiary-analyzer-stat-bee-cost = Цена новой пчелы
apiary-analyzer-stat-lay-rate = Скорость размножения
apiary-analyzer-stat-lifetime = Жизнь вне улья
apiary-analyzer-stat-scout-finds = Лотков за вылет
apiary-analyzer-stat-mutation = Мутация при опылении
apiary-analyzer-units = { $amount } ед.
apiary-analyzer-bees = Пчёлы
apiary-analyzer-nectar = Нектар
apiary-analyzer-combs = Соты
apiary-analyzer-health = Прочность
apiary-analyzer-bees-home = Дома
apiary-analyzer-new-bee = Потомство
apiary-analyzer-queen = Матка
apiary-analyzer-egg = Яйцо
apiary-analyzer-yes = есть
apiary-analyzer-no = нет
apiary-analyzer-frames-none = Рамок нет.
apiary-analyzer-frame-combs = { $combs } из { $max } сот
apiary-analyzer-trays-none = Разведчица ещё не нашла ни одного лотка.
apiary-analyzer-tray-empty = пустой лоток
apiary-analyzer-tray-ripe = Растение спелое, рабочие его посещают.
apiary-analyzer-tray-growing = Растение ещё растёт.
apiary-analyzer-tray-blind = Лоток пустой или рабочие до него сейчас не долетят.
apiary-analyzer-of = { $value } из { $max }
apiary-analyzer-row-slots = Слоты признаков
apiary-analyzer-row-payload = Реагенты, ед.
apiary-analyzer-row-hive = Улей
apiary-analyzer-hive-known = цел
apiary-analyzer-hive-lost = разрушен
apiary-analyzer-row-memory = Помнит лотков
apiary-analyzer-row-charges = Опылений осталось
beekeeping-take-queen-done = Вы осторожно достаёте матку.
apiary-analyzer-trait-separator = {": "}
beekeeping-queen-examine-dying = [color=red]Без улья матка слабеет и скоро погибнет.[/color]
beekeeping-queen-died = Матка погибла без улья.
beekeeping-frame-upgraded = Вы укрепляете рамку воском. Теперь она вмещает { $max } сот.
beekeeping-frame-upgrade-max = Рамка уже вмещает { $max } сот, больше не выйдет.
beekeeping-flatpack-examine = Используйте [color=yellow]лом[/color], чтобы распаковать это.
apiary-analyzer-nectar-idle =
    Нектар [color=#9A9A9A]не тратится[/color]: в улье нет матки.
    Вставьте [bold]матку[/bold], и улей оживёт.
apiary-analyzer-nectar-bees =
    Нектар [color=#FFA500]почти не копится[/color]: его [bold]сразу тратят[/bold].
    Сейчас он копится на [color=#FFD700]новых пчёл[/color], сотам нет места.
apiary-analyzer-nectar-beesandcombs =
    Нектар [color=#FFA500]почти не копится[/color]: его [bold]сразу тратят[/bold].
    Матка делает [color=#FFD700]пчёл[/color], остальное идёт в [color=#F0E68C]соты[/color].
apiary-analyzer-nectar-combs =
    Нектар [color=#FFA500]почти не копится[/color]: его [bold]сразу тратят[/bold].
    Сейчас он уходит на [color=#F0E68C]соты[/color].
apiary-analyzer-nectar-framesfull =
    Нектар [color=#FFA500]копится[/color]: тратить его некуда.
    Все рамки полные, [bold]срежьте соты[/bold].
apiary-analyzer-nectar-noworkers =
    Нектар [color=#FFA500]копится[/color]: соты строить некому.
    Все рабочие сейчас не дома.
apiary-analyzer-foraging = В полёте
apiary-analyzer-nectar-noframes =
    Нектар [color=#FFA500]копится[/color]: тратить его некуда.
    В улье [bold]нет рамок[/bold], вставьте их.
beekeeping-take-scout-tired = Разведчица отдыхает после вылета. Попробуйте позже.
beekeeping-take-workers-tired = Все свободные рабочие отдыхают после работы. Попробуйте позже.
apiary-analyzer-brood-going = есть
apiary-analyzer-brood-stopped = нет
apiary-analyzer-stat-egg-wait = Ожидание яйца
apiary-analyzer-stat-egg-life = Жизнь яйца
apiary-analyzer-stat-egg-cooldown = Кулдаун яйца
bee-trait-early-egg = Ранняя яйцекладка
bee-trait-early-egg-desc = Полная колония быстрее откладывает яйцо матки.
bee-trait-hardy-egg = Живучее яйцо
bee-trait-hardy-egg-desc = Яйцо матки живёт дольше, больше времени, чтобы собрать молочко.
bee-trait-swift-queen = Скорая матка
bee-trait-swift-queen-desc = После яйца новое появляется быстрее.
bee-trait-lazy-queen = Вялая матка
bee-trait-lazy-queen-desc = Яйцо матки откладывается заметно позже.
bee-trait-fragile-egg = Хрупкое яйцо
bee-trait-fragile-egg-desc = Яйцо матки живёт вдвое меньше, молочко нужно собирать быстро.
beekeeping-hive-examine-swarm-modifier = Рой был особенный: [color={ $color }]{ $name }[/color]. { $desc }
swarm-modifier-plain = Обычный рой
swarm-modifier-plain-desc = Ничем не примечателен.
swarm-modifier-shy = Робкий рой
swarm-modifier-shy-desc = Ушла только треть колонии.
swarm-modifier-homebody = Домосед
swarm-modifier-homebody-desc = Вентиляцию не искал и поселился рядом с ульем.
swarm-modifier-rich = Богатый улей
swarm-modifier-rich-desc = Вмещает 12 сот вместо 8.
swarm-modifier-huge = Крупный рой
swarm-modifier-huge-desc = Ушли три четверти колонии.
swarm-modifier-wanderer = Странник
swarm-modifier-wanderer-desc = Искал вентиляцию и гнездо вдвое дальше.
swarm-modifier-tainted = Заражённый улей
swarm-modifier-tainted-desc = В сотах что-то подозрительное.
apiary-analyzer-row-instability = Нестабильность
