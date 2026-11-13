#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using Solitaire.Core.Cards;
using Solitaire.Core.Klondike;
using Solitaire.Core.Rules;

public partial class Table : VBoxContainer
{
    const int NtfWmCloseRequest = 1000;
    const int NtfApplicationFocusOut = 2011;
    const int NtfApplicationPaused = 2014;

    readonly Random _rng = new();
    KlondikeState? _game;
    GameRulesConfig _rules = GameRulesConfig.ClassicWindows;

    Label _scoreLabel = null!;
    Label _timerLabel = null!;
    Button _undoButton = null!;
    Button _newGameButton = null!;
    Button _settingsButton = null!;
    Control _playArea = null!;
    Control _topBarPanel = null!;
    Control _topRow = null!;
    Control _tableauRow = null!;
    Control _stockCell = null!;
    Control _wasteCell = null!;
    readonly TextureRect[] _foundationSlots = new TextureRect[4];
    readonly Control[] _tableauColumns = new Control[7];
    AcceptDialog _invariantDialog = null!;
    Label _gameEndTitle = null!;
    Label _gameEndParty0 = null!;
    Label _gameEndParty1 = null!;
    Label _gameEndParty2 = null!;
    Label _gameEndParty3 = null!;
    Label _gameEndBest0 = null!;
    Label _gameEndBest1 = null!;
    Label _gameEndBest2 = null!;
    Label _gameEndBest3 = null!;
    Label _gameEndBest4 = null!;
    Control _hasBestGroup = null!;
    Control _gameEndDialog = null!;
    Window _settingsWindow = null!;
    OptionButton _stockDrawOption = null!;
    OptionButton _scoringOption = null!;
    OptionButton _screenOrientationOption = null!;
    OptionButton _languageOption = null!;
    CheckBox _vegasCumulativeCheck = null!;
    CheckBox _timedGameCheck = null!;
    Control _cardStash = null!;
    Control _dragHand = null!;
    readonly Dictionary<Card, CardNode> _cardPool = new();
    readonly List<KlondikeState> _undoStack = new(4);

    Vector2 _baseCardSize = new(80, 112);
    Vector2 _cardSize = new(80, 112);
    float _tableauOverlap = 28f;
    float _wasteFanDx = 18f;
    double _matchElapsedSeconds;

    enum DragSource { None, Tableau, Waste, Foundation }
    DragSource _dragFrom;
    int _dragColumn;
    int _dragFoundationPile = -1;
    int _dragStartIndex;
    Vector2 _dragOffset;
    bool _isDragging;

    const float DragThresholdPixels = 12f;
    bool _pointerActive;
    bool _pointerIsTouch;
    bool _dragActivated;
    Vector2 _pointerDownPos;

    public override void _Ready()
    {
        I18n.Init();
        Input.EmulateMouseFromTouch = true;
        const string bar = "TopBarPanel/MarginContainer/TopBar/";
        _scoreLabel = GetNode<Label>(bar + "ScoreTimeRow/ScoreLabel");
        _timerLabel = GetNode<Label>(bar + "ScoreTimeRow/TimerLabel");
        _undoButton = GetNode<Button>(bar + "ScoreTimeRow/UndoButton");
        _newGameButton = GetNode<Button>(bar + "ActionsRow/NewDealButton");
        _settingsButton = GetNode<Button>(bar + "ActionsRow/SettingsButton");
        _playArea = GetNode<Control>("PlayArea");
        _topBarPanel = GetNode<Control>("TopBarPanel");
        _playArea.ClipContents = true;
        _topRow = GetNode<Control>("PlayArea/PlayAreaCenter/PlayStack/TopRow");
        _tableauRow = GetNode<Control>("PlayArea/PlayAreaCenter/PlayStack/TableauRow");
        _tableauRow.ClipContents = true;
        _stockCell = GetNode<Control>("PlayArea/PlayAreaCenter/PlayStack/TopRow/StockWasteBlock/StockCell");
        _wasteCell = GetNode<Control>("PlayArea/PlayAreaCenter/PlayStack/TopRow/StockWasteBlock/WasteCell");
        for (int p = 0; p < 4; p++)
            _foundationSlots[p] = GetNode<TextureRect>($"PlayArea/PlayAreaCenter/PlayStack/TopRow/RightBox/F{p}");
        for (int c = 0; c < 3; c++)
            _tableauColumns[c] = GetNode<Control>($"PlayArea/PlayAreaCenter/PlayStack/TableauRow/TableauLeft/T{c}");
        for (int c = 3; c < 7; c++)
            _tableauColumns[c] = GetNode<Control>($"PlayArea/PlayAreaCenter/PlayStack/TableauRow/TableauRight/T{c}");

        SetContainersMouseIgnore();
        _invariantDialog = GetNode<AcceptDialog>("InvariantDialog");
        const string endV = "GameEndLayer/GameEndDialog/DialogCenter/DialogPanel/Margin/VBox";
        _gameEndDialog = GetNode<Control>("GameEndLayer/GameEndDialog");
        const string pStack = "GameEndPartyStack/";
        const string bSt = "HasBestGroup/GameEndBestStack/";
        _gameEndTitle = GetNode<Label>(endV + "/GameEndTitle");
        _gameEndParty0 = GetNode<Label>(endV + "/" + pStack + "Party0");
        _gameEndParty1 = GetNode<Label>(endV + "/" + pStack + "Party1");
        _gameEndParty2 = GetNode<Label>(endV + "/" + pStack + "Party2");
        _gameEndParty3 = GetNode<Label>(endV + "/" + pStack + "Party3");
        _hasBestGroup = GetNode<Control>(endV + "/HasBestGroup");
        _gameEndBest0 = GetNode<Label>(endV + "/" + bSt + "Best0");
        _gameEndBest1 = GetNode<Label>(endV + "/" + bSt + "Best1");
        _gameEndBest2 = GetNode<Label>(endV + "/" + bSt + "Best2");
        _gameEndBest3 = GetNode<Label>(endV + "/" + bSt + "Best3");
        _gameEndBest4 = GetNode<Label>(endV + "/" + bSt + "Best4");
        GetNode<Button>(endV + "/ButtonRow/GameEndClose").Pressed += OnGameEndClose;
        GetNode<Button>(endV + "/ButtonRow/GameEndAgain").Pressed += OnGameEndAgain;
        const string settingsVBox = "SettingsLayer/SettingsWindow/Margin/Scroll/VBox/";
        _settingsWindow = GetNode<Window>("SettingsLayer/SettingsWindow");
        _stockDrawOption = GetNode<OptionButton>(settingsVBox + "StockDrawOption");
        _scoringOption = GetNode<OptionButton>(settingsVBox + "ScoringOption");
        _vegasCumulativeCheck = GetNode<CheckBox>(settingsVBox + "VegasCumulativeCheck");
        _timedGameCheck = GetNode<CheckBox>(settingsVBox + "TimedGameCheck");
        _screenOrientationOption = GetNode<OptionButton>(settingsVBox + "ScreenOrientationOption");
        _languageOption = GetNode<OptionButton>(settingsVBox + "LanguageOption");
        FillOptionButtonStockScoringOrientation();
        _languageOption.ItemSelected += OnLanguageItemSelected;
        if (!CanApplyScreenOrientation())
        {
            GetNode<Control>(settingsVBox + "OrientationLabel").Visible = false;
            _screenOrientationOption.Visible = false;
        }
        GetNode<Button>(settingsVBox + "PresetRow/BtnPresetClassic").Pressed += OnPresetClassicWindows;
        GetNode<Button>(settingsVBox + "PresetRow/BtnPresetRelax").Pressed += OnPresetDrawOneRelax;
        GetNode<Button>(settingsVBox + "PresetRow/BtnPresetVegas").Pressed += OnPresetVegasSession;
        GetNode<Button>(settingsVBox + "ButtonRow/BtnSavePlayOut").Pressed += OnSettingsSavePlayOut;
        GetNode<Button>(settingsVBox + "ButtonRow/BtnNewGame").Pressed += OnSettingsNewGame;
        GetNode<Button>(settingsVBox + "ButtonRow/BtnCancel").Pressed += OnSettingsCancel;
        _settingsWindow.CloseRequested += OnSettingsWindowCloseRequested;
        _dragHand = GetNode<Control>("DragLayer/DragHand");
        _cardStash = new Control { Name = "CardStash", Visible = false };
        AddChild(_cardStash);

        _undoButton.Pressed += OnUndoPressed;

        _newGameButton.Pressed += OnNewGamePressed;
        _settingsButton.Pressed += OnSettingsPressed;
        _invariantDialog.Confirmed += OnInvariantDialogConfirmed;

        Resized += OnResized;
        SetProcess(false);
        ApplyAllUiText();
        CallDeferred(nameof(DeferredSetup));
    }

    public override void _Process(double delta)
    {
        if (!_rules.TimedGame || _game == null)
            return;
        if (_game.IsGameWon())
            return;
        _matchElapsedSeconds += delta;
        UpdateTimerDisplay();
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == NtfWmCloseRequest
            || what == NtfApplicationFocusOut
            || what == NtfApplicationPaused)
            PersistActiveMatch();
    }

    void PersistActiveMatch()
    {
        if (_game is null) return;
        ActiveMatchStore.Save(_game, _matchElapsedSeconds);
    }

    void SetContainersMouseIgnore()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        GetNode<Control>("TopBarPanel/MarginContainer/TopBar").MouseFilter = MouseFilterEnum.Stop;
        _playArea.MouseFilter = MouseFilterEnum.Ignore;
        GetNode<Control>("PlayArea/PlayAreaCenter").MouseFilter = MouseFilterEnum.Ignore;
        _topRow.MouseFilter = MouseFilterEnum.Ignore;
        GetNode<Control>("PlayArea/PlayAreaCenter/PlayStack/TopRow/StockWasteBlock").MouseFilter = MouseFilterEnum.Ignore;
        GetNode<Control>("PlayArea/PlayAreaCenter/PlayStack/TopRow/RightBox").MouseFilter = MouseFilterEnum.Ignore;
        _tableauRow.MouseFilter = MouseFilterEnum.Ignore;
        GetNode<Control>("PlayArea/PlayAreaCenter/PlayStack/TableauRow/TableauLeft").MouseFilter = MouseFilterEnum.Ignore;
        GetNode<Control>("PlayArea/PlayAreaCenter/PlayStack/TableauRow/TableauRight").MouseFilter = MouseFilterEnum.Ignore;
        _stockCell.MouseFilter = MouseFilterEnum.Ignore;
        _wasteCell.MouseFilter = MouseFilterEnum.Ignore;
        for (int p = 0; p < 4; p++)
            _foundationSlots[p].MouseFilter = MouseFilterEnum.Ignore;
        for (int c = 0; c < 7; c++)
            _tableauColumns[c].MouseFilter = MouseFilterEnum.Ignore;
    }

    void ScheduleRefreshView() => Callable.From(RefreshView).CallDeferred();

    void DeferredSetup()
    {
        LoadBaseCardSizeFromTexture();
        UpdateLayoutFromViewport();
        CreateCardPool();
        ApplyCellCardSizes();
        _rules = UserSettingsStore.LoadRulesOrDefaults();
        UserSettingsStore.WriteTemplateIfMissing(_rules);
        ApplyScreenOrientation(UserSettingsStore.LoadScreenOrientationOrDefault());
        if (ActiveMatchStore.TryLoad(
                out KlondikeState? loaded, out double elapsed, out GameRulesConfig fromMatch))
        {
            _game = loaded;
            _rules = fromMatch;
            _matchElapsedSeconds = elapsed;
            ClearUndoStack();
        }
        else
            DealNewGame();
        CallDeferred(nameof(DeferredLayoutAfterFirstFrame));
    }

    void DeferredLayoutAfterFirstFrame()
    {
        if (!UpdateLayoutFromViewport())
            return;
        if (_cardPool.Count == 0) return;
        foreach (CardNode n in _cardPool.Values) n.SetCardSize(_cardSize);
        ApplyCellCardSizes();
        if (_game != null) RefreshView();
    }

    void CreateCardPool()
    {
        foreach (Suit suit in new[] { Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades })
        {
            for (int r = (int)Rank.Ace; r <= (int)Rank.King; r++)
            {
                var card = new Card(suit, (Rank)r);
                var node = CardNode.Create(card, _cardSize);
                _cardPool[card] = node;
                _cardStash.AddChild(node);
            }
        }
    }

    void OnResized()
    {
        if (UpdateLayoutFromViewport() && _cardPool.Count > 0)
        {
            foreach (CardNode n in _cardPool.Values) n.SetCardSize(_cardSize);
            ApplyCellCardSizes();
        }
        ScheduleRefreshView();
        CallDeferred(nameof(DeferredLayoutAfterFirstFrame));
    }

    void OnInvariantDialogConfirmed()
    {
    }

    void LoadBaseCardSizeFromTexture()
    {
        _baseCardSize = new(80, 112);
        string path = CardPaths.FacePng(new Card(Suit.Hearts, Rank.Ace));
        var tex = ResourceLoader.Load<Texture2D>(path);
        if (tex != null)
        {
            Vector2 s = tex.GetSize();
            float scale = Mathf.Min(100f / s.X, 140f / s.Y);
            _baseCardSize = s * scale;
        }
    }

    static float DefaultTableauOverlapForCardSize(Vector2 cardSize) =>
        Mathf.Clamp(cardSize.Y * 0.20f, 22f, cardSize.Y * 0.42f);

    float GetTableauAvailableHeight()
    {
        float playH = _playArea.Size.Y;
        if (playH < 1f)
        {
            float vh = GetViewport().GetVisibleRect().Size.Y;
            var topBar = GetNodeOrNull<Control>("TopBarPanel");
            float barH = topBar?.Size.Y ?? 56f;
            playH = Mathf.Max(0f, vh - barH);
        }
        float topH = _topRow.Size.Y >= 1f ? _topRow.Size.Y : _topRow.CustomMinimumSize.Y;
        return Mathf.Max(0f, playH - topH - 4f);
    }

    void RecalculateTableauOverlapForTableauPiles()
    {
        if (_game is null)
            return;
        int maxPile = 1;
        for (int c = 0; c < 7; c++)
        {
            int n = _game.Tableau[c].Count;
            if (n > maxPile) maxPile = n;
        }
        _tableauOverlap = DefaultTableauOverlapForCardSize(_cardSize);
        if (maxPile <= 1)
            return;
        float available = GetTableauAvailableHeight();
        if (available < 1f)
            return;
        float need = _cardSize.Y + (maxPile - 1) * _tableauOverlap;
        if (need <= available)
            return;
        float room = available - _cardSize.Y;
        if (room <= 0f)
        {
            _tableauOverlap = 8f;
            return;
        }
        const float minOverlap = 8f;
        _tableauOverlap = Mathf.Max(minOverlap, room / (maxPile - 1));
    }

    bool UpdateLayoutFromViewport()
    {
        ApplyScreenPadding();
        float w = GetViewport().GetVisibleRect().Size.X;
        if (w < 32f)
        {
            _cardSize = _baseCardSize;
            _tableauOverlap = DefaultTableauOverlapForCardSize(_baseCardSize);
            _wasteFanDx = Mathf.Max(8f, _baseCardSize.X * 0.34f);
            return false;
        }
        float wPerCol = Mathf.Min(w * 0.94f / 7f, 180f);
        float ar = _baseCardSize.X > 0.01f ? _baseCardSize.Y / _baseCardSize.X : 1.4f;
        // On a wide screen the width alone would give cards too tall for the table: keep room
        // for the top row plus a dealt column of seven cards (one card + six 20% overlaps).
        float barH = _topBarPanel.Size.Y > 1f ? _topBarPanel.Size.Y : 60f;
        float playH = GetViewport().GetVisibleRect().Size.Y - barH;
        float maxCardH = (playH - 40f) / 3.3f;
        if (maxCardH > 1f && wPerCol * ar > maxCardH)
            wPerCol = maxCardH / ar;
        wPerCol = Mathf.Max(wPerCol, 18f);
        _cardSize = new Vector2(wPerCol, wPerCol * ar);
        _tableauOverlap = DefaultTableauOverlapForCardSize(_cardSize);
        _wasteFanDx = Mathf.Max(6f, _cardSize.X * 0.34f);
        return true;
    }

    // Phones with rounded screen corners clip whatever sits right at the edge, so the
    // top bar keeps extra room on both sides. The camera cutout is ignored on purpose:
    // cards rarely reach it and are wide enough not to be hidden by it.
    const int TopBarMarginX = 10;
    const int MobileCornerPaddingX = 28;

    const int BaseFontSize = 22;

    // The viewport is stretched from a 720 x 900 portrait base, so in landscape a phone scales
    // everything by its height and the text gets smaller than in portrait. Scale the theme font
    // back up so text keeps the same physical size in both orientations.
    void ApplyUiScale()
    {
        Theme? theme = ThemeDB.GetProjectTheme();
        if (theme is null) return;
        float factor = 1f;
        Vector2I win = DisplayServer.WindowGetSize();
        if (OS.HasFeature("mobile") && win.X > 0 && win.Y > 0)
        {
            float portraitScale = Mathf.Min(win.X, win.Y) / 720f;
            float currentScale = Mathf.Min(win.X / 720f, win.Y / 900f);
            factor = Mathf.Clamp(portraitScale / currentScale, 1f, 1.5f);
        }
        int size = Mathf.RoundToInt(BaseFontSize * factor);
        if (theme.DefaultFontSize != size)
            theme.DefaultFontSize = size;
        _settingsWindow?.AddThemeFontSizeOverride("title_font_size", size);
    }

    void ApplyScreenPadding()
    {
        ApplyUiScale();
        int x = TopBarMarginX + (OS.HasFeature("mobile") ? MobileCornerPaddingX : 0);
        var bar = GetNode<MarginContainer>("TopBarPanel/MarginContainer");
        bar.AddThemeConstantOverride("margin_left", x);
        bar.AddThemeConstantOverride("margin_right", x);
    }

    void ApplyCellCardSizes()
    {
        Vector2 min = _cardSize;
        _stockCell.CustomMinimumSize = min;
        _stockCell.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _stockCell.SizeFlagsStretchRatio = 1f;

        _wasteCell.CustomMinimumSize = new Vector2(min.X * 2f, min.Y);
        _wasteCell.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _wasteCell.SizeFlagsStretchRatio = 2f;

        for (int p = 0; p < 4; p++)
        {
            _foundationSlots[p].CustomMinimumSize = new Vector2(min.X, min.Y);
            _foundationSlots[p].Texture = null;
            _foundationSlots[p].SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        }

        for (int c = 0; c < 7; c++)
        {
            _tableauColumns[c].CustomMinimumSize = min;
            _tableauColumns[c].SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _tableauColumns[c].ClipContents = true;
        }

        SyncSlotShadowsExceptWaste();

        const float minTopH = 120f;
        _topRow.CustomMinimumSize = new Vector2(0, Mathf.Max(minTopH, min.Y + 8f));
        _topRow.SizeFlagsVertical = Control.SizeFlags.Fill;
        _tableauRow.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
    }

    void OnNewGamePressed()
    {
        CancelDrag();
        _rules = UserSettingsStore.LoadRulesOrDefaults();
        DealNewGame();
    }

    void OnSettingsPressed()
    {
        PopulateSettingsFromFile();
        _settingsWindow.PopupCentered(SettingsWindowSize());
        Callable.From(FitSettingsWindow).CallDeferred();
    }

    void PopulateSettingsFromFile()
    {
        I18n.SetLanguageCode(UserSettingsStore.LoadLanguageOrNull());
        ApplyAllUiText();
        ApplyRulesToSettingsUi(UserSettingsStore.LoadRulesOrDefaults());
        if (CanApplyScreenOrientation())
            ApplyOrientationToSettingsUi(UserSettingsStore.LoadScreenOrientationOrDefault());
        ApplyLanguageToSettingsUi(UserSettingsStore.LoadLanguageOrNull());
    }

    void ApplyRulesToSettingsUi(GameRulesConfig r)
    {
        _stockDrawOption.Selected = r.StockDraw == StockDrawMode.DrawOne ? 0 : 1;
        _scoringOption.Selected = (int)r.Scoring;
        _vegasCumulativeCheck.ButtonPressed = r.VegasCumulative;
        _timedGameCheck.ButtonPressed = r.TimedGame;
    }

    void ApplyOrientationToSettingsUi(DisplayServer.ScreenOrientation o)
    {
        _screenOrientationOption.Selected = o switch
        {
            DisplayServer.ScreenOrientation.Landscape => 1,
            DisplayServer.ScreenOrientation.Sensor => 2,
            _ => 0,
        };
    }

    void ApplyLanguageToSettingsUi(string? stored)
    {
        _languageOption.Selected = stored switch
        {
            I18n.LocaleEn => 1,
            I18n.LocaleRu => 2,
            I18n.LocaleZh => 3,
            _ => 0,
        };
    }

    string? ReadLanguageFromUi() =>
        _languageOption.Selected switch
        {
            1 => I18n.LocaleEn,
            2 => I18n.LocaleRu,
            3 => I18n.LocaleZh,
            _ => null,
        };

    void OnLanguageItemSelected(long _)
    {
        string? lang = ReadLanguageFromUi();
        I18n.SetLanguageCode(lang);
        var rules = ReadSettingsFromUi();
        var orientation = ReadScreenOrientationFromUi();
        UserSettingsStore.Save(rules, orientation, lang);
        ApplyAllUiText();
        ApplyRulesToSettingsUi(rules);
        if (CanApplyScreenOrientation())
            ApplyOrientationToSettingsUi(orientation);
        ApplyLanguageToSettingsUi(lang);
        if (_game != null) RefreshView();
    }

    void FillOptionButtonStockScoringOrientation()
    {
        int stockSel = _stockDrawOption.Selected;
        int scSel = _scoringOption.Selected;
        int orSel = _screenOrientationOption.Selected;
        _stockDrawOption.Clear();
        _stockDrawOption.AddItem(I18n.T("settings.drawOne"), 0);
        _stockDrawOption.AddItem(I18n.T("settings.drawThree"), 1);
        _scoringOption.Clear();
        _scoringOption.AddItem(I18n.T("settings.scoringNone"), 0);
        _scoringOption.AddItem(I18n.T("settings.scoringStandard"), 1);
        _scoringOption.AddItem(I18n.T("settings.scoringVegas"), 2);
        _screenOrientationOption.Clear();
        _screenOrientationOption.AddItem(I18n.T("settings.orientPortrait"), 0);
        _screenOrientationOption.AddItem(I18n.T("settings.orientLandscape"), 1);
        _screenOrientationOption.AddItem(I18n.T("settings.orientSensor"), 2);
        _stockDrawOption.Selected = Math.Clamp(stockSel, 0, 1);
        _scoringOption.Selected = Math.Clamp(scSel, 0, 2);
        _screenOrientationOption.Selected = Math.Clamp(orSel, 0, 2);
    }

    void FillLanguageOptionItems()
    {
        int sel = _languageOption.Selected;
        _languageOption.Clear();
        _languageOption.AddItem(I18n.T("settings.langAuto"), 0);
        _languageOption.AddItem(I18n.T("settings.langEn"), 1);
        _languageOption.AddItem(I18n.T("settings.langRu"), 2);
        _languageOption.AddItem(I18n.T("settings.langZh"), 3);
        _languageOption.Selected = Math.Clamp(sel, 0, 3);
    }

    void ApplyAllUiText()
    {
        const string v = "SettingsLayer/SettingsWindow/Margin/Scroll/VBox/";
        _newGameButton.Text = I18n.T("hud.newGame");
        _settingsButton.Text = I18n.T("hud.settings");
        _undoButton.Text = I18n.T("hud.undo");
        _settingsWindow.Title = I18n.T("settings.title");
        GetNode<Label>(v + "LanguageLabel").Text = I18n.T("settings.language");
        GetNode<Label>(v + "StockLabel").Text = I18n.T("settings.stock");
        GetNode<Label>(v + "ScoringLabel").Text = I18n.T("settings.scoring");
        _vegasCumulativeCheck.Text = I18n.T("settings.vegasCumulative");
        _timedGameCheck.Text = I18n.T("settings.timedGame");
        GetNode<Label>(v + "OrientationLabel").Text = I18n.T("settings.orientation");
        GetNode<Label>(v + "PresetLabel").Text = I18n.T("settings.presets");
        GetNode<Button>(v + "PresetRow/BtnPresetClassic").Text = I18n.T("settings.presetClassic");
        GetNode<Button>(v + "PresetRow/BtnPresetRelax").Text = I18n.T("settings.presetRelaxed");
        GetNode<Button>(v + "PresetRow/BtnPresetVegas").Text = I18n.T("settings.presetVegas");
        GetNode<Button>(v + "ButtonRow/BtnSavePlayOut").Text = I18n.T("settings.playOut");
        GetNode<Button>(v + "ButtonRow/BtnNewGame").Text = I18n.T("settings.newDeal");
        GetNode<Button>(v + "ButtonRow/BtnCancel").Text = I18n.T("settings.cancel");
        GetNode<LinkButton>(v + "DevLinkRow/DevSiteLink").Text = I18n.T("settings.devLink");
        FillLanguageOptionItems();
        FillOptionButtonStockScoringOrientation();
        _invariantDialog.Title = I18n.T("invariant.title");
        _invariantDialog.DialogText = I18n.T("invariant.message");
        _invariantDialog.OkButtonText = I18n.T("invariant.ok");
        GetNode<Button>("GameEndLayer/GameEndDialog/DialogCenter/DialogPanel/Margin/VBox/ButtonRow/GameEndClose")
            .Text = I18n.T("win.close");
        GetNode<Button>("GameEndLayer/GameEndDialog/DialogCenter/DialogPanel/Margin/VBox/ButtonRow/GameEndAgain")
            .Text = I18n.T("win.playAgain");
        GetNode<Label>("GameEndLayer/GameEndDialog/DialogCenter/DialogPanel/Margin/VBox/GameEndTitle")
            .Text = I18n.T("win.title");
    }

    static bool CanApplyScreenOrientation() =>
        OS.GetName() is "Android" or "iOS";

    bool ShouldIgnoreForGameTableInput(Vector2 viewportPos)
    {
        if (_topBarPanel.GetGlobalRect().HasPoint(viewportPos)) return true;
        if (_settingsWindow.Visible) return true;
        if (_invariantDialog.Visible) return true;
        if (_gameEndDialog.Visible) return true;
        return false;
    }

    static bool IsMobilePlatform() =>
        OS.HasFeature("mobile");

    static void ApplyScreenOrientation(DisplayServer.ScreenOrientation o)
    {
        if (!CanApplyScreenOrientation())
            return;
        DisplayServer.ScreenSetOrientation(o);
    }

    void OnPresetClassicWindows() => ApplyRulesToSettingsUi(GameRulesConfig.ClassicWindows);

    void OnPresetDrawOneRelax()
    {
        ApplyRulesToSettingsUi(new GameRulesConfig(
            StockDrawMode.DrawOne,
            ScoringMode.None,
            VegasCumulative: false,
            TimedGame: false));
    }

    void OnPresetVegasSession()
    {
        _stockDrawOption.Selected = 1;
        _scoringOption.Selected = (int)ScoringMode.Vegas;
        _timedGameCheck.ButtonPressed = false;
    }

    GameRulesConfig ReadSettingsFromUi()
    {
        StockDrawMode stock = _stockDrawOption.Selected == 0 ? StockDrawMode.DrawOne : StockDrawMode.DrawThree;
        var scoring = (ScoringMode)_scoringOption.Selected;
        return new GameRulesConfig(
            stock,
            scoring,
            _vegasCumulativeCheck.ButtonPressed,
            _timedGameCheck.ButtonPressed);
    }

    DisplayServer.ScreenOrientation ReadScreenOrientationFromUi()
    {
        if (!CanApplyScreenOrientation())
            return UserSettingsStore.LoadScreenOrientationOrDefault();
        return _screenOrientationOption.Selected switch
        {
            1 => DisplayServer.ScreenOrientation.Landscape,
            2 => DisplayServer.ScreenOrientation.Sensor,
            _ => DisplayServer.ScreenOrientation.Portrait,
        };
    }

    // Fit the settings window to its content, but never taller than the screen:
    // in landscape on a phone the rest scrolls inside the window.
    Vector2I SettingsWindowSize()
    {
        var margin = GetNode<MarginContainer>("SettingsLayer/SettingsWindow/Margin");
        Vector2 content = GetNode<Control>("SettingsLayer/SettingsWindow/Margin/Scroll/VBox").GetCombinedMinimumSize();
        float padX = margin.GetThemeConstant("margin_left") + margin.GetThemeConstant("margin_right");
        float padY = margin.GetThemeConstant("margin_top") + margin.GetThemeConstant("margin_bottom");
        Vector2 vp = GetViewport().GetVisibleRect().Size;
        float title = _settingsWindow.GetThemeConstant("title_height");
        float w = Mathf.Min(Mathf.Max(content.X + padX + 24f, 560f), vp.X - 24f);
        float h = Mathf.Min(content.Y + padY + 8f, vp.Y - title - 24f);
        return new Vector2I(Mathf.RoundToInt(w), Mathf.RoundToInt(h));
    }

    // Texts are refreshed right before the popup, so measure again once they are laid out.
    void FitSettingsWindow()
    {
        if (!_settingsWindow.Visible) return;
        Vector2I size = SettingsWindowSize();
        if (size == _settingsWindow.Size) return;
        _settingsWindow.Size = size;
        _settingsWindow.MoveToCenter();
    }

    void OnSettingsSavePlayOut()
    {
        var rules = ReadSettingsFromUi();
        var orientation = ReadScreenOrientationFromUi();
        string? lang = ReadLanguageFromUi();
        I18n.SetLanguageCode(lang);
        UserSettingsStore.Save(rules, orientation, lang);
        ApplyScreenOrientation(orientation);
        ApplyAllUiText();
        if (_game != null) RefreshView();
        _settingsWindow.Hide();
    }

    void OnSettingsNewGame()
    {
        GameRulesConfig cfg = ReadSettingsFromUi();
        var orientation = ReadScreenOrientationFromUi();
        string? lang = ReadLanguageFromUi();
        I18n.SetLanguageCode(lang);
        UserSettingsStore.Save(cfg, orientation, lang);
        ApplyScreenOrientation(orientation);
        _rules = cfg;
        CancelDrag();
        DealNewGame();
        ApplyAllUiText();
        if (_game != null) RefreshView();
        _settingsWindow.Hide();
    }

    void OnSettingsCancel() => _settingsWindow.Hide();

    void OnSettingsWindowCloseRequested() => _settingsWindow.Hide();

    void DealNewGame()
    {
        ClearUndoStack();
        _matchElapsedSeconds = 0;
        int? vegasCarry = null;
        if (_game != null && _rules.Scoring == ScoringMode.Vegas && _rules.VegasCumulative)
            vegasCarry = _game.Score;

        _game = KlondikeState.NewShuffled(_rules, _rng, vegasCarry);
        if (!_game.AreInvariantsHeld())
            _game = KlondikeState.NewShuffled(_rules, _rng, vegasCarry);
        RefreshView();
        PersistActiveMatch();
    }

    void TryAdvanceAndRefresh()
    {
        if (_game == null) return;
        KlondikeState before = _game.Clone();
        if (!_game.TryAdvanceStock()) return;
        PushUndo(before);
        AfterKernelCall();
    }

    void AfterKernelCall()
    {
        if (_game == null) return;
        if (_game.AreInvariantsHeld())
        {
            RefreshView();
            PersistActiveMatch();
            CheckGameEnd();
            return;
        }
        ClearUndoStack();
        _matchElapsedSeconds = 0;
        _game = KlondikeState.NewShuffled(_rules, _rng, vegasCarriedBalance: null);
        RefreshView();
        PersistActiveMatch();
        _invariantDialog.PopupCentered();
    }

    void CheckGameEnd()
    {
        if (_game is null || !_game.IsGameWon()) return;
        ClearUndoStack();
        UpdateUndoButton();

        var result = new MatchResult
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Score = _game.Score,
            Moves = _game.Moves,
            ElapsedSeconds = _matchElapsedSeconds,
        };
        ResultsUpdate update = ResultsStore.Record(_rules, result);

        _gameEndTitle.Text = I18n.T("win.title");
        ApplyGameEndParty(_game);
        if (GameEndHasBestSection(update))
        {
            ApplyGameEndBest(update);
            _hasBestGroup.Visible = true;
        }
        else
        {
            _hasBestGroup.Visible = false;
        }
        Callable.From(ShowGameEndDialog).CallDeferred();
    }

    void ShowGameEndDialog() => _gameEndDialog.Visible = true;

    string FormatGameRulesForWinDialog(GameRulesConfig r)
    {
        string deal = r.StockDraw == StockDrawMode.DrawOne
            ? I18n.T("rules.deal1")
            : I18n.T("rules.deal3");
        string sc = r.Scoring switch
        {
            ScoringMode.None => I18n.T("rules.scoringNone"),
            ScoringMode.Standard => I18n.T("rules.scoringStd"),
            ScoringMode.Vegas => r.VegasCumulative
                ? I18n.T("rules.vegasCumulative")
                : I18n.T("rules.vegas"),
            _ => I18n.T("rules.emDash"),
        };
        string t = r.TimedGame ? I18n.T("rules.timed") : I18n.T("rules.notTimed");
        return $"{deal}, {sc}, {t}";
    }

    bool GameEndHasBestSection(ResultsUpdate update) =>
        (_rules.Scoring != ScoringMode.None && update.BestScore.HasValue)
        || update.BestMoves.HasValue
        || (_rules.TimedGame && update.BestElapsedSeconds.HasValue);

    void ApplyGameEndParty(KlondikeState game)
    {
        _gameEndParty0.Text = I18n.T("win.party");
        _gameEndParty1.Text = I18n.Tf("win.score", game.Score);
        _gameEndParty2.Text = I18n.Tf("win.moves", game.Moves);
        if (_rules.TimedGame)
        {
            _gameEndParty3.Text = I18n.Tf("win.time", FormatTime(_matchElapsedSeconds));
            _gameEndParty3.Visible = true;
        }
        else
        {
            _gameEndParty3.Visible = false;
        }
    }

    void ApplyGameEndBest(ResultsUpdate update)
    {
        _gameEndBest0.Text = I18n.T("win.bestHeader");
        _gameEndBest1.Text = $"({FormatGameRulesForWinDialog(_rules)})";
        bool a = _rules.Scoring != ScoringMode.None && update.BestScore.HasValue;
        bool b = update.BestMoves.HasValue;
        bool c = _rules.TimedGame && update.BestElapsedSeconds.HasValue;
        if (a)
        {
            _gameEndBest2.Text = I18n.Tf("win.bestRecordScore", update.BestScore!.Value);
            if (update.NewScoreRecord) _gameEndBest2.Text += I18n.T("win.newRecord");
            _gameEndBest2.Visible = true;
        }
        else
        {
            _gameEndBest2.Text = string.Empty;
            _gameEndBest2.Visible = false;
        }
        if (b)
        {
            _gameEndBest3.Text = I18n.Tf("win.bestRecordMoves", update.BestMoves!.Value);
            if (update.NewMovesRecord) _gameEndBest3.Text += I18n.T("win.newRecord");
            _gameEndBest3.Visible = true;
        }
        else
        {
            _gameEndBest3.Text = string.Empty;
            _gameEndBest3.Visible = false;
        }
        if (c)
        {
            _gameEndBest4.Text = I18n.Tf("win.bestRecordTime", FormatTime(update.BestElapsedSeconds!.Value));
            if (update.NewTimeRecord) _gameEndBest4.Text += I18n.T("win.newRecord");
            _gameEndBest4.Visible = true;
        }
        else
        {
            _gameEndBest4.Text = string.Empty;
            _gameEndBest4.Visible = false;
        }
    }

    void OnGameEndClose() => _gameEndDialog.Hide();

    void OnGameEndAgain()
    {
        _gameEndDialog.Hide();
        _rules = UserSettingsStore.LoadRulesOrDefaults();
        CancelDrag();
        DealNewGame();
    }

    static string FormatTime(double seconds)
    {
        int t = (int)seconds;
        if (t < 0) t = 0;
        int m = t / 60;
        int s = t % 60;
        return $"{m}:{s:D2}";
    }

    void PushUndo(KlondikeState snapshotBeforeMove)
    {
        _undoStack.Add(snapshotBeforeMove);
        const int maxDepth = 3;
        while (_undoStack.Count > maxDepth)
            _undoStack.RemoveAt(0);
    }

    void ClearUndoStack() => _undoStack.Clear();

    void UpdateUndoButton() => _undoButton.Disabled = _undoStack.Count == 0;

    void OnUndoPressed()
    {
        if (_undoStack.Count == 0 || _game == null)
            return;
        CancelDrag();
        int last = _undoStack.Count - 1;
        _game = _undoStack[last];
        _undoStack.RemoveAt(last);
        RefreshView();
        PersistActiveMatch();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_game == null) return;

        switch (@event)
        {
            case InputEventMouseButton mb when mb.ButtonIndex == MouseButton.Left:
                if (mb.Pressed)
                {
                    if (mb.DoubleClick && !IsMobilePlatform())
                        OnMouseDoubleClick(mb.GlobalPosition);
                    else
                        OnPointerDown(mb.GlobalPosition, isTouch: IsMobilePlatform());
                }
                else
                {
                    OnPointerUp(mb.GlobalPosition);
                }
                break;
            case InputEventMouseMotion mm:
                OnPointerMove(mm.GlobalPosition);
                break;
        }
    }

    void OnPointerDown(Vector2 pos, bool isTouch)
    {
        if (_pointerActive) return;
        if (ShouldIgnoreForGameTableInput(pos)) return;
        _pointerActive = true;
        _pointerIsTouch = isTouch;
        _pointerDownPos = pos;
        _dragActivated = false;
    }

    void OnMouseDoubleClick(Vector2 pos)
    {
        if (_isDragging) return;
        _pointerActive = false;
        _dragActivated = false;
        HandleAutoToFoundation(pos);
    }

    void OnPointerMove(Vector2 pos)
    {
        if (!_pointerActive) return;
        if (!_dragActivated)
        {
            if ((pos - _pointerDownPos).Length() < DragThresholdPixels) return;
            _dragActivated = true;
            TryBeginDrag(_pointerDownPos);
        }
        if (_isDragging)
            _dragHand.GlobalPosition = pos - _dragOffset;
    }

    void OnPointerUp(Vector2 pos)
    {
        if (!_pointerActive) return;
        if (_isDragging)
        {
            EndDrag(pos);
        }
        else if (!_dragActivated)
        {
            if (_stockCell.GetGlobalRect().HasPoint(_pointerDownPos))
                TryAdvanceAndRefresh();
            else if (_pointerIsTouch)
                HandleAutoToFoundation(_pointerDownPos);
        }
        _pointerActive = false;
        _dragActivated = false;
    }

    void HandleAutoToFoundation(Vector2 pos)
    {
        if (TryTapTableauAutoToFoundation(pos)) return;
        TryTapWasteAutoToFoundation(pos);
    }

    bool TryTapTableauAutoToFoundation(Vector2 pos)
    {
        for (int c = 0; c < 7; c++)
        {
            Rect2 rect = _tableauColumns[c].GetGlobalRect();
            if (!rect.HasPoint(pos)) continue;
            var pile = _game!.Tableau[c];
            if (pile.Count == 0) return true;

            float localY = pos.Y - rect.Position.Y;
            int idx = (int)(localY / _tableauOverlap);
            idx = Math.Clamp(idx, 0, pile.Count - 1);
            if (idx < pile.Count - 1)
            {
                float cardBottom = idx * _tableauOverlap + _cardSize.Y;
                if (localY > cardBottom)
                    idx = Math.Min(idx + 1, pile.Count - 1);
            }
            if (idx != pile.Count - 1) return true;
            if (!pile[idx].IsFaceUp) return true;

            KlondikeState before = _game.Clone();
            if (_game.TryMoveTableauToFoundation(c))
            {
                PushUndo(before);
                AfterKernelCall();
            }
            return true;
        }
        return false;
    }

    bool TryTapWasteAutoToFoundation(Vector2 pos)
    {
        if (!_wasteCell.GetGlobalRect().HasPoint(pos)) return false;
        if (_game!.Waste.Count == 0) return true;

        KlondikeState before = _game.Clone();
        if (_game.TryMoveWasteToFoundation())
        {
            PushUndo(before);
            AfterKernelCall();
        }
        return true;
    }

    void TryBeginDrag(Vector2 globalPos)
    {
        if (TryPickTableau(globalPos)) return;
        if (TryPickWaste(globalPos)) return;
        if (TryPickFoundation(globalPos)) return;
    }

    bool TryPickTableau(Vector2 globalPos)
    {
        for (int c = 0; c < 7; c++)
        {
            Rect2 rect = _tableauColumns[c].GetGlobalRect();
            if (!rect.HasPoint(globalPos)) continue;

            var pile = _game!.Tableau[c];
            if (pile.Count == 0) continue;

            float localY = globalPos.Y - rect.Position.Y;
            int idx = (int)(localY / _tableauOverlap);
            idx = Math.Clamp(idx, 0, pile.Count - 1);

            if (idx < pile.Count - 1)
            {
                float cardBottom = idx * _tableauOverlap + _cardSize.Y;
                if (localY > cardBottom)
                    idx = Math.Min(idx + 1, pile.Count - 1);
            }

            if (!pile[idx].IsFaceUp) return false;

            _dragFrom = DragSource.Tableau;
            _dragColumn = c;
            _dragStartIndex = idx;
            BeginDragVisual(globalPos, c, idx);
            return true;
        }
        return false;
    }

    bool TryPickWaste(Vector2 globalPos)
    {
        Rect2 rect = _wasteCell.GetGlobalRect();
        if (!rect.HasPoint(globalPos)) return false;
        if (_game!.Waste.Count == 0) return false;

        _dragFrom = DragSource.Waste;
        _dragColumn = -1;
        _dragStartIndex = _game.Waste.Count - 1;

        Card wasteTop = _game.Waste[^1];
        CardNode cn = _cardPool[wasteTop];
        Vector2 cardGlobal = cn.GlobalPosition;
        _dragOffset = globalPos - cardGlobal;

        cn.GetParent()?.RemoveChild(cn);
        _dragHand.AddChild(cn);
        cn.Position = Vector2.Zero;
        cn.ZIndex = 0;

        _isDragging = true;
        _dragHand.GlobalPosition = globalPos - _dragOffset;
        return true;
    }

    bool TryPickFoundation(Vector2 globalPos)
    {
        for (int p = 0; p < 4; p++)
        {
            if (!_foundationSlots[p].GetGlobalRect().HasPoint(globalPos))
                continue;
            if (_game!.Foundation[p].Count == 0)
                return false;

            _dragFrom = DragSource.Foundation;
            _dragFoundationPile = p;
            _dragColumn = -1;

            Card top = _game.Foundation[p][^1];
            CardNode cn = _cardPool[top];
            Vector2 cardGlobal = cn.GlobalPosition;
            _dragOffset = globalPos - cardGlobal;

            cn.GetParent()?.RemoveChild(cn);
            _dragHand.AddChild(cn);
            cn.Position = Vector2.Zero;
            cn.ZIndex = 0;
            cn.ShowFace();

            _isDragging = true;
            _dragHand.GlobalPosition = globalPos - _dragOffset;
            return true;
        }
        return false;
    }

    void BeginDragVisual(Vector2 globalPos, int column, int startIndex)
    {
        var pile = _game!.Tableau[column];
        CardNode firstNode = _cardPool[pile[startIndex].Card];
        Vector2 cardGlobal = firstNode.GlobalPosition;
        _dragOffset = globalPos - cardGlobal;

        for (int i = startIndex; i < pile.Count; i++)
        {
            CardNode cn = _cardPool[pile[i].Card];
            cn.GetParent()?.RemoveChild(cn);
            _dragHand.AddChild(cn);
            cn.Position = new Vector2(0, (i - startIndex) * _tableauOverlap);
            cn.ZIndex = i - startIndex;
            cn.ShowFace();
        }

        _isDragging = true;
        _dragHand.GlobalPosition = globalPos - _dragOffset;
    }

    void EndDrag(Vector2 globalPos)
    {
        int targetCol = HitTestTableauColumn(globalPos);
        int targetFound = HitTestFoundation(globalPos);

        KlondikeState before = _game!.Clone();
        bool moved = false;

        if (_dragFrom == DragSource.Tableau)
        {
            if (targetCol >= 0 && targetCol != _dragColumn)
                moved = _game.TryMoveTableauRun(_dragColumn, _dragStartIndex, targetCol);
            else if (targetFound >= 0)
                moved = _game.TryMoveTableauToFoundation(_dragColumn);
        }
        else if (_dragFrom == DragSource.Waste)
        {
            if (targetCol >= 0)
                moved = _game.TryMoveWasteToTableau(targetCol);
            else if (targetFound >= 0)
                moved = _game.TryMoveWasteToFoundation();
        }
        else if (_dragFrom == DragSource.Foundation)
        {
            if (targetCol >= 0)
                moved = _game.TryMoveFoundationToTableau(_dragFoundationPile, targetCol);
        }

        CancelDrag();

        if (moved)
        {
            PushUndo(before);
            AfterKernelCall();
        }
        else
            RefreshView();
    }

    void CancelDrag()
    {
        if (!_isDragging) return;
        foreach (Node child in _dragHand.GetChildren())
        {
            _dragHand.RemoveChild(child);
            if (child is CardNode cn)
                _cardStash.AddChild(cn);
        }
        _isDragging = false;
        _dragFrom = DragSource.None;
        _dragFoundationPile = -1;
    }

    int HitTestTableauColumn(Vector2 globalPos)
    {
        for (int c = 0; c < 7; c++)
        {
            if (_tableauColumns[c].GetGlobalRect().HasPoint(globalPos))
                return c;
        }
        return -1;
    }

    int HitTestFoundation(Vector2 globalPos)
    {
        for (int p = 0; p < 4; p++)
        {
            if (_foundationSlots[p].GetGlobalRect().HasPoint(globalPos))
                return p;
        }
        return -1;
    }

    void StashAllCards()
    {
        foreach (CardNode cn in _cardPool.Values)
        {
            if (cn.GetParent() == _cardStash)
                continue;
            cn.GetParent()?.RemoveChild(cn);
            _cardStash.AddChild(cn);
        }
    }

    void PlaceCard(Control parent, CardNode node, Vector2 pos, int z, bool faceUp)
    {
        if (node.GetParent() != null)
            node.GetParent().RemoveChild(node);
        parent.AddChild(node);
        node.Position = pos;
        node.ZIndex = z;
        if (faceUp) node.ShowFace();
        else node.ShowBack();
    }

    static void ClearDecorations(Control parent, params string[] reserved)
    {
        var keep = new HashSet<string>(reserved, StringComparer.Ordinal);
        var snapshot = parent.GetChildren();
        foreach (Node child in snapshot)
        {
            if (child is CardNode)
                continue;
            if ((string)child.Name == "SlotShadow")
                continue;
            if (keep.Contains((string)child.Name))
                continue;
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }

    static void SyncSlotShadow(Control parent, Vector2 size, Vector2 pos)
    {
        var shadow = parent.GetNodeOrNull<ColorRect>("SlotShadow");
        if (shadow == null) return;
        shadow.Position = pos;
        shadow.Size = size;
    }

    void SyncSlotShadowsExceptWaste()
    {
        SyncSlotShadow(_stockCell, _cardSize, Vector2.Zero);
        for (int p = 0; p < 4; p++)
            SyncSlotShadow(_foundationSlots[p], _cardSize, Vector2.Zero);
        for (int c = 0; c < 7; c++)
            SyncSlotShadow(_tableauColumns[c], _cardSize, Vector2.Zero);
    }

    void UpdateTimerDisplay()
    {
        bool show = _rules.TimedGame && _game != null;
        _timerLabel.Visible = show;
        if (!show)
            return;
        int t = (int)_matchElapsedSeconds;
        int m = t / 60;
        int s = t % 60;
        _timerLabel.Text = I18n.Tf("hud.timer", $"{m}:{s:D2}");
    }

    // Reserve room for the widest likely value so the Undo button does not move when
    // the score or the clock changes (depends on language and the current font size).
    static void ReserveLabelWidth(Label label, string widestText)
    {
        Font font = label.GetThemeFont("font");
        int size = label.GetThemeFontSize("font_size");
        float w = Mathf.Ceil(font.GetStringSize(widestText, HorizontalAlignment.Left, -1, size).X) + 4f;
        if (!Mathf.IsEqualApprox(label.CustomMinimumSize.X, w))
            label.CustomMinimumSize = new Vector2(w, label.CustomMinimumSize.Y);
    }

    void RefreshView()
    {
        if (_game == null)
            return;

        RecalculateTableauOverlapForTableauPiles();

        ReserveLabelWidth(_scoreLabel, I18n.Tf("hud.score", "-8888"));
        ReserveLabelWidth(_timerLabel, I18n.Tf("hud.timer", "88:88"));
        _scoreLabel.Text = I18n.Tf("hud.score", _game.Score);
        UpdateTimerDisplay();
        SetProcess(_rules.TimedGame);

        StashAllCards();

        ClearDecorations(_stockCell);
        ClearDecorations(_wasteCell);
        for (int p = 0; p < 4; p++)
            ClearDecorations(_foundationSlots[p]);
        for (int c = 0; c < 7; c++)
            ClearDecorations(_tableauColumns[c]);

        DrawStockPile();
        DrawWastePile();
        DrawFoundationPiles();
        for (int c = 0; c < 7; c++)
            DrawTableauColumn(c);

        UpdateUndoButton();
    }

    void DrawStockPile()
    {
        int n = _game!.Stock.Count;
        if (n > 0)
        {
            CardNode topNode = _cardPool[_game.Stock[^1]];
            PlaceCard(_stockCell, topNode, Vector2.Zero, 1, false);
        }
    }

    void DrawWastePile()
    {
        float cellW = _wasteCell.Size.X > 0f ? _wasteCell.Size.X : _wasteCell.CustomMinimumSize.X;
        float stockPad = _cardSize.X * 0.12f;
        float rightEdge = Mathf.Max(0f, cellW - _cardSize.X - stockPad);

        SyncSlotShadow(_wasteCell, _cardSize, new Vector2(rightEdge, 0));

        int n = _game!.Waste.Count;
        if (n == 0) return;

        int count = Math.Min(3, n);
        int from = n - count;
        for (int i = from; i < n; i++)
        {
            int visIdx = n - 1 - i;
            float fx = rightEdge - visIdx * _wasteFanDx;
            CardNode cn = _cardPool[_game.Waste[i]];
            PlaceCard(_wasteCell, cn, new Vector2(fx, 0), 20 + i, true);
        }
    }

    void DrawFoundationPiles()
    {
        for (int p = 0; p < 4; p++)
        {
            TextureRect slot = _foundationSlots[p];
            var pile = _game!.Foundation[p];
            if (pile.Count == 0) continue;

            CardNode topNode = _cardPool[pile[^1]];
            PlaceCard(slot, topNode, Vector2.Zero, 1, true);
        }
    }

    void DrawTableauColumn(int columnIndex)
    {
        Control col = _tableauColumns[columnIndex];
        var pile = _game!.Tableau[columnIndex];
        int count = pile.Count;
        float stackSpan = count <= 1 ? 0f : (count - 1) * _tableauOverlap;
        float contentHeight = _cardSize.Y + stackSpan;
        float minColumnH = contentHeight + 8f;
        col.CustomMinimumSize = new Vector2(_cardSize.X, Mathf.Max(_cardSize.Y, minColumnH));

        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            PlacedCard pc = pile[i];
            float yTop = i * _tableauOverlap;
            CardNode cn = _cardPool[pc.Card];
            PlaceCard(col, cn, new Vector2(0, yTop), 50 + i, pc.IsFaceUp);
        }
    }
}
