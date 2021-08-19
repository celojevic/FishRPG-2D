using FishNet.Component.Animating;
using FishNet.Object;
using UnityEngine;

public class PlayerVisuals : NetworkBehaviour
{

    [SerializeField] private SpriteRenderer _sr = null;

    /// <summary>
    /// The master Player script.
    /// </summary>
    private Player _player;
    private Animation _currentAnimation;
    private NetworkAnimator _netAnimator;

    private void Awake()
    {
        _player = GetComponent<Player>();
        _netAnimator = GetComponent<NetworkAnimator>();
        if (_sr == null)
            GetComponentInChildren<SpriteRenderer>();
        AssignClassData();
    }

    void AssignClassData()
    {
        if (_player.Class == null) return;

        var app = _player.GetAppearance();
        _sr.sprite = app.Sprite;
        _netAnimator.SetController(app.Controller);
    }

    private void Update()
    {
        if (!IsOwner) return;

        CheckSpriteFlip();
        CheckAnimation();
    }

    void CheckSpriteFlip()
    {
        if (_player.Input.InputVector.x < 0)
        {
            FlipSprite(true);
        }
        else if (_player.Input.InputVector.x > 0)
        {
            FlipSprite(false);
        }
    }

    void FlipSprite(bool flip)
    {
        if (_sr.flipX == flip) return;

        _sr.flipX = flip;
    }

    void CheckAnimation()
    {
        if (_player.Input.InputVector != Vector2.zero)
        {
            ChangeAnimation(Animation.Walk);
        }
        else
        {
            ChangeAnimation(Animation.Idle);
        }
    }

    void ChangeAnimation(Animation animation)
    {
        if (_currentAnimation == animation) return;

        _currentAnimation = animation;
        _netAnimator.Play(animation.ToString());
    }

    private enum Animation : byte
    {
        Idle,
        Walk
    }

}

