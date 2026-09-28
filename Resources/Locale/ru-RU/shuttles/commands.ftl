# FTLdiskburner
cmd-ftldisk-desc = Создаёт диск FTL-координат для перелёта на карту, где находится сущность с указанным EntityID
cmd-ftldisk-help = ftldisk [EntityID]

cmd-ftldisk-no-transform = У сущности { $destination } нет компонента Transform!
cmd-ftldisk-no-map = У сущности { $destination } нет карты!
cmd-ftldisk-no-map-comp = Сущность { $destination } находится на карте { $map }, у которой нет компонента карты.
cmd-ftldisk-map-not-init = Сущность { $destination } находится на неинициализированной карте { $map }! Убедитесь, что карту безопасно инициализировать, и сначала инициализируйте её, иначе игроки не смогут двигаться!
cmd-ftldisk-map-paused = Сущность { $desintation } находится на приостановленной карте { $map }! Сначала снимите карту с паузы, иначе игроки не смогут двигаться.
cmd-ftldisk-planet = Сущность { $desintation } находится на планетарной карте { $map }, для которой нужна точка FTL. Возможно, она уже существует.
cmd-ftldisk-already-dest-not-enabled = Сущность { $destination } находится на карте { $map }, где уже есть FTLDestinationComponent, но он отключён! В целях безопасности включите его вручную.
cmd-ftldisk-requires-ftl-point = Сущность { $destination } находится на карте { $map }, для перелёта на которую нужна точка FTL! Возможно, она уже существует.

cmd-ftldisk-hint = Сетевой идентификатор карты
