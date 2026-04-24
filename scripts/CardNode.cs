#nullable enable
using Godot;
using Solitaire.Core.Cards;

public partial class CardNode : Control
{
    const string BackTexturePath = "res://sprites/cards/Vardo.png";

    static Texture2D? _backTexCache;

    public Card CardId { get; private set; }

    TextureRect _face = null!;
    TextureRect _back = null!;

    bool _isFaceUp;

    CardNode() { }

    static Texture2D? GetBackTexture()
    {
        _backTexCache ??= ResourceLoader.Load<Texture2D>(BackTexturePath);
        return _backTexCache;
    }

    public static CardNode Create(Card card, Vector2 cardSize)
    {
        var node = new CardNode();
        node.CardId = card;
        node.Name = $"Card_{card.Suit}_{(int)card.Rank}";
        node.MouseFilter = MouseFilterEnum.Ignore;
        node.Size = cardSize;
        node.CustomMinimumSize = cardSize;

        string path = CardPaths.FacePng(card);
        var faceTex = ResourceLoader.Load<Texture2D>(path);

        node._face = new TextureRect
        {
            Size = cardSize,
            CustomMinimumSize = cardSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Texture = faceTex,
            MouseFilter = MouseFilterEnum.Ignore,
        };

        node._back = new TextureRect
        {
            Size = cardSize,
            CustomMinimumSize = cardSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Texture = GetBackTexture(),
            MouseFilter = MouseFilterEnum.Ignore,
        };

        node.AddChild(node._face);
        node.AddChild(node._back);

        node._face.Visible = false;
        node._back.Visible = true;
        node._isFaceUp = false;

        return node;
    }

    public void ShowFace()
    {
        if (_isFaceUp) return;
        _isFaceUp = true;
        _face.Visible = true;
        _back.Visible = false;
    }

    public void ShowBack()
    {
        if (!_isFaceUp) return;
        _isFaceUp = false;
        _face.Visible = false;
        _back.Visible = true;
    }

    public void SetCardSize(Vector2 cardSize)
    {
        Size = cardSize;
        CustomMinimumSize = cardSize;

        _face.Size = cardSize;
        _face.CustomMinimumSize = cardSize;

        _back.Size = cardSize;
        _back.CustomMinimumSize = cardSize;
    }
}
