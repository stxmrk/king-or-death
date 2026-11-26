using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace KingOrDeath
{
    // shortcut: in-code EN/RU table, move to the Localization package when a third language or translators appear.
    public static class Loc
    {
        public static event Action Changed;

        static readonly Dictionary<string, string[]> table = new Dictionary<string, string[]>
        {
            ["menu.new"] = new[] { "New Game", "Новая игра" },
            ["menu.continue"] = new[] { "Continue", "Продолжить" },
            ["menu.load"] = new[] { "Load", "Загрузить" },
            ["menu.settings"] = new[] { "Settings", "Настройки" },
            ["menu.about"] = new[] { "About", "Об игре" },
            ["menu.quit"] = new[] { "Quit", "Выход" },
            ["menu.hint"] = new[] { "Arrows — select · Enter — confirm · Esc — back", "Стрелки — выбор · Enter — подтвердить · Esc — назад" },
            ["common.back"] = new[] { "Back · Esc", "Назад · Esc" },
            ["scn.title"] = new[] { "New Game", "Новая игра" },
            ["scn.list"] = new[] { "SCENARIOS", "СЦЕНАРИИ" },
            ["scn.frontier.meta"] = new[] { "Tutorial · 10–15 min", "Обучение · 10–15 мин" },
            ["scn.siege"] = new[] { "Siege of Stone Ford", "Осада Каменного брода" },
            ["scn.siege.meta"] = new[] { "Unlocks in v0.3", "Откроется в v0.3" },
            ["scn.skirmish"] = new[] { "Skirmish", "Свободная игра" },
            ["scn.skirmish.meta"] = new[] { "Random map · later", "Случайная карта · позже" },
            ["scn.frontier.desc"] = new[]
            {
                "Capture a frontier post, supply the keep and hold the bridge. The fallen stay on the field — and draw wolves out of the forest.",
                "Захватите пограничный пост, наладьте снабжение крепости и удержите мост. Павшие остаются на поле — и привлекают волков из леса.",
            },
            ["scn.tags"] = new[] { "2 posts · 1 bridge · Wolf den · Raider wave", "2 поста · 1 мост · Логово волков · Волна рейдеров" },
            ["scn.hints"] = new[] { "Tutorial hints", "Подсказки обучения" },
            ["scn.start"] = new[] { "Start", "Начать" },
            ["load.building"] = new[] { "Building the world…", "Строим мир…" },
            ["tip.1"] = new[]
            {
                "The world remembers. Unburied bodies start to smell after 16 seconds — bury or burn them, or wolves will follow the scent to your workers.",
                "Мир помнит. Непогребённые тела начинают пахнуть через 16 секунд. Похороните или сожгите их — иначе волки придут на запах и встретят ваших рабочих.",
            },
            ["tip.2"] = new[]
            {
                "Resources are physical. Wood cut at an outpost stays in its store until a caravan carries it to the keep.",
                "Ресурсы физические. Дерево, добытое у поста, лежит на его складе, пока обоз не довезёт его до крепости.",
            },
            ["tip.3"] = new[]
            {
                "A caravan without escort is prey. Assign swordsmen to guard it on dangerous roads.",
                "Обоз без охраны — добыча. Выделите мечников для сопровождения на опасных дорогах.",
            },
            ["tip.4"] = new[]
            {
                "Only swordsmen capture flags. An enemy in the radius contests the capture.",
                "Флаги захватывают только мечники. Противник в радиусе оспаривает захват.",
            },
            ["about.title"] = new[] { "About", "Об игре" },
            ["about.text"] = new[]
            {
                "KING OR DEATH is a single-player fantasy RTS where the world remembers every battle: the fallen stay where they fell, and the land answers.\n\nThis is an early prototype. Models are procedural; menu art was generated with GPT Image 2. Fonts: Cormorant SC and PT Sans (SIL Open Font License).",
                "KING OR DEATH — однопользовательская фэнтезийная RTS, где мир помнит каждую битву: павшие остаются там, где пали, и земля на это отвечает.\n\nЭто ранний прототип. Модели процедурные; арт меню сгенерирован GPT Image 2. Шрифты: Cormorant SC и PT Sans (SIL Open Font License).",
            },
            ["pause.title"] = new[] { "Paused", "Пауза" },
            ["pause.resume"] = new[] { "Resume", "Продолжить" },
            ["pause.save"] = new[] { "Save", "Сохранить" },
            ["pause.restart"] = new[] { "Restart", "Перезапустить" },
            ["pause.mainmenu"] = new[] { "Main Menu", "В главное меню" },
            ["pause.quit"] = new[] { "Quit Game", "Выйти из игры" },
            ["pause.warn"] = new[] { "Unsaved progress will be lost.\nEsc — back", "Несохранённый прогресс будет потерян.\nEsc — вернуться" },
            ["set.title"] = new[] { "Settings", "Настройки" },
            ["set.tab.video"] = new[] { "Display", "Изображение" },
            ["set.tab.audio"] = new[] { "Audio", "Звук" },
            ["set.tab.controls"] = new[] { "Controls", "Управление" },
            ["set.tab.game"] = new[] { "Game", "Игра" },
            ["set.window"] = new[] { "Window mode", "Режим окна" },
            ["win.windowed"] = new[] { "Windowed", "В окне" },
            ["win.borderless"] = new[] { "Borderless", "Без рамки" },
            ["win.fullscreen"] = new[] { "Fullscreen", "Полный экран" },
            ["set.resolution"] = new[] { "Resolution", "Разрешение" },
            ["set.quality"] = new[] { "Quality", "Качество" },
            ["set.vsync"] = new[] { "Vertical sync", "Вертикальная синхронизация" },
            ["set.fps"] = new[] { "Frame limit", "Ограничение FPS" },
            ["fps.none"] = new[] { "Unlimited", "Без ограничения" },
            ["set.master"] = new[] { "Master volume", "Общая громкость" },
            ["set.audio.note"] = new[] { "The prototype has no sound yet.", "В прототипе пока нет звука." },
            ["set.language"] = new[] { "Language", "Язык" },
            ["set.hints"] = new[] { "Tutorial hints", "Подсказки обучения" },
            ["set.reset"] = new[] { "Reset", "Сбросить" },
            ["set.apply"] = new[] { "Apply", "Применить" },
            ["q.Very Low"] = new[] { "Very Low", "Очень низкое" },
            ["q.Low"] = new[] { "Low", "Низкое" },
            ["q.Medium"] = new[] { "Medium", "Среднее" },
            ["q.High"] = new[] { "High", "Высокое" },
            ["q.Very High"] = new[] { "Very High", "Очень высокое" },
            ["q.Ultra"] = new[] { "Ultra", "Ультра" },
            ["ctl.camera"] = new[] { "Move camera", "Перемещение камеры" },
            ["ctl.camera.k"] = new[] { "WASD / arrows / middle mouse", "WASD / стрелки / средняя кнопка" },
            ["ctl.zoom"] = new[] { "Zoom, rotate", "Масштаб, поворот" },
            ["ctl.zoom.k"] = new[] { "Mouse wheel, Q / E", "Колесо мыши, Q / E" },
            ["ctl.home"] = new[] { "Keep / overview", "К крепости / общий вид" },
            ["ctl.select"] = new[] { "Select", "Выделение" },
            ["ctl.select.k"] = new[] { "LMB, drag box; Shift adds", "ЛКМ, рамка; Shift добавляет" },
            ["ctl.army"] = new[] { "Select whole army", "Выбрать всю армию" },
            ["ctl.order"] = new[] { "Move / attack / gather", "Движение / атака / добыча" },
            ["ctl.order.k"] = new[] { "RMB", "ПКМ" },
            ["ctl.build"] = new[] { "Build house", "Построить дом" },
            ["ctl.pause"] = new[] { "Pause", "Пауза" },
            ["ctl.pause.k"] = new[] { "Space", "Пробел" },
            ["ctl.menu"] = new[] { "Menu", "Меню" },
            ["hud.tagline"] = new[] { "A WORLD REMEMBERS EVERY BATTLE", "МИР ПОМНИТ КАЖДУЮ БИТВУ" },
            ["hud.scenario"] = new[] { "THE FIRST FRONTIER", "ПЕРВЫЙ РУБЕЖ" },
            ["hud.stock"] = new[] { "WOOD {0} · STONE {1} · FOOD {2}", "ДЕРЕВО {0} · КАМЕНЬ {1} · ЕДА {2}" },
            ["hud.pause"] = new[] { "Pause", "Пауза" },
            ["hud.resume"] = new[] { "Resume", "Дальше" },
            ["hud.help"] = new[] { "Help", "Справка" },
            ["hud.paused"] = new[] { "PAUSED", "ПАУЗА" },
            ["hud.help.title"] = new[] { "YOUR FIRST ORDERS", "ПЕРВЫЕ ПРИКАЗЫ" },
            ["hud.help.1"] = new[] { "1. Select army [1], right-click the northern flag.", "1. Выберите армию [1], ПКМ по северному флагу." },
            ["hud.help.2"] = new[] { "2. Capture Pineward, then choose Wood crew.", "2. Захватите Pineward, затем нажмите «Лесорубы»." },
            ["hud.help.3"] = new[] { "3. Start caravan; defend it or bury the fallen.", "3. Запустите обоз; охраняйте его и хороните павших." },
            ["hud.selected"] = new[] { "{0} SELECTED", "ВЫБРАНО: {0}" },
            ["hud.command"] = new[] { "FRONTIER COMMAND", "КОМАНДОВАНИЕ РУБЕЖА" },
            ["hud.cargo"] = new[] { "Cargo", "Груз" },
            ["hud.rmb.corpse"] = new[] { "RMB corpse:", "ПКМ по телу:" },
            ["hud.bury"] = new[] { "Bury", "Похоронить" },
            ["hud.burn"] = new[] { "Burn", "Сжечь" },
            ["hud.hide"] = new[] { "Hide", "Спрятать" },
            ["hud.carry"] = new[] { "Carry", "Нести" },
            ["hud.drop"] = new[] { "Drop body", "Бросить тело" },
            ["hud.sword.hint"] = new[] { "RMB move / attack · Shift adds selection", "ПКМ — движение / атака · Shift добавляет" },
            ["hud.escort"] = new[] { "Escort caravan", "Охранять обоз" },
            ["hud.idle.1"] = new[] { "Select soldiers, workers or a local store.", "Выберите солдат, рабочих или склад." },
            ["hud.idle.2"] = new[] { "1 — army · B — build · Space — pause", "1 — армия · B — стройка · Пробел — пауза" },
            ["hud.idle.3"] = new[] { "WASD pan · Wheel zoom · Q/E rotate", "WASD — камера · Колесо — масштаб · Q/E — поворот" },
            ["hud.idle.4"] = new[] { "Drag a frame to select several units.", "Растяните рамку, чтобы выбрать нескольких." },
            ["hud.dev"] = new[] { "LOCAL DEVELOPMENT", "РАЗВИТИЕ НА МЕСТЕ" },
            ["hud.build"] = new[] { "Build house · 50 W / 20 S", "Дом · 50 Д / 20 К" },
            ["hud.recruit"] = new[] { "Recruit sword · 20 W / 25 F", "Мечник · 20 Д / 25 Е" },
            ["hud.crew"] = new[] { "Wood crew", "Лесорубы" },
            ["hud.raid"] = new[] { "Call raid", "Вызвать набег" },
            ["hud.dev.note"] = new[] { "Houses do not extend territory.", "Дома не расширяют территорию." },
            ["hud.caravan"] = new[] { "CARAVAN / PHYSICAL LOGISTICS", "ОБОЗ / ФИЗИЧЕСКАЯ ЛОГИСТИКА" },
            ["hud.caravan.lost"] = new[] { "Caravan lost. Salvage cargo with workers.", "Обоз потерян. Соберите груз рабочими." },
            ["hud.no.source"] = new[] { "No source", "Нет источника" },
            ["hud.no.destination"] = new[] { "No destination", "Нет цели" },
            ["hud.route.pine"] = new[] { "Pineward › Keep", "Pineward › Крепость" },
            ["hud.route.east"] = new[] { "Eastwatch › Keep", "Eastwatch › Крепость" },
            ["hud.stop"] = new[] { "Stop", "Стоп" },
            ["hud.reverse"] = new[] { "Reverse", "Обратно" },
            ["hud.select.caravan"] = new[] { "Select caravan", "Выбрать обоз" },
            ["hud.place.cancel"] = new[] { "RMB / Esc cancels", "ПКМ / Esc — отмена" },
            ["hud.scent"] = new[] { "SCENT", "ЗАПАХ" },
            ["hud.pack.hunt"] = new[] { "PACK HUNTING", "СТАЯ НА ОХОТЕ" },
            ["hud.pack.den"] = new[] { "PACK AT DEN", "СТАЯ В ЛОГОВЕ" },
            ["hud.n.escort"] = new[] { "Selected swordsmen are escorting the caravan.", "Выбранные мечники охраняют обоз." },
            ["hud.n.recruit"] = new[] { "Not enough local resources, or store is not yours.", "Не хватает ресурсов на складе, или склад не ваш." },
            ["hud.n.route.capture"] = new[] { "Capture this post before opening its trade route.", "Сначала захватите этот пост." },
            ["hud.n.route.both"] = new[] { "Both route stores must be controlled.", "Оба склада маршрута должны быть вашими." },
            ["hud.n.crew.capture"] = new[] { "Capture Pineward before assigning its wood crew.", "Сначала захватите Pineward." },
            ["hud.n.crew"] = new[] { "Two workers assigned to Pineward's woodland. Wood stays in its local store.", "Двое рабочих рубят лес у Pineward. Дерево остаётся на местном складе." },
            ["obj.orders"] = new[] { "Command your troops", "Отдайте приказ войскам" },
            ["obj.capture"] = new[] { "Capture a frontier post", "Захватите пограничный пост" },
            ["obj.build"] = new[] { "Build within your territory", "Постройте на своей земле" },
            ["obj.harvest"] = new[] { "Deliver a worker's harvest", "Доставьте добычу рабочего" },
            ["obj.load"] = new[] { "Load the caravan", "Загрузите обоз" },
            ["obj.delivery"] = new[] { "Complete a trade delivery", "Завершите доставку" },
            ["obj.battle"] = new[] { "Fight the Ashen host", "Сразитесь с Пепельным войском" },
            ["obj.remains"] = new[] { "See the fallen remain", "Павшие остаются на поле" },
            ["obj.wolves"] = new[] { "Watch wolves follow the scent", "Волки идут на запах" },
            ["obj.cleanup"] = new[] { "Clear the remains", "Уберите останки" },
            ["faction.Neutral"] = new[] { "Neutral", "Ничей" },
            ["faction.Player"] = new[] { "Crown", "Корона" },
            ["faction.Enemy"] = new[] { "Ashen", "Пепельные" },
            ["faction.Wildlife"] = new[] { "Wild", "Дикие" },
            ["res.Wood"] = new[] { "Wood", "Дерево" },
            ["res.Stone"] = new[] { "Stone", "Камень" },
            ["res.Food"] = new[] { "Food", "Еда" },
            ["corpse.Bury"] = new[] { "Bury", "Похоронить" },
            ["corpse.Burn"] = new[] { "Burn", "Сжечь" },
            ["corpse.Hide"] = new[] { "Hide", "Спрятать" },
            ["corpse.Carry"] = new[] { "Carry", "Нести" },
            ["corpse.Leave"] = new[] { "Leave", "Оставить" },
        };

        public static bool Has(string key) => table.ContainsKey(key);

        public static string T(string key) => T(key, key);

        public static string T(string key, string fallback) =>
            table.TryGetValue(key, out var v) ? v[GameSettings.Language == "ru" ? 1 : 0] : fallback;

        public static void SetLanguage(string language)
        {
            if (language == GameSettings.Language)
                return;
            GameSettings.Language = language;
            Changed?.Invoke();
        }

        // UXML stores keys as text; the key is kept in userData so a language switch can re-translate.
        public static void Apply(VisualElement root)
        {
            root.Query<TextElement>()
                .ForEach(te =>
                {
                    if (!(te.userData is string key))
                    {
                        if (!Has(te.text))
                            return;
                        key = te.text;
                        te.userData = key;
                    }
                    te.text = T(key);
                });
        }
    }
}
