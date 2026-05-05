using Godot;

namespace Maze.Views;

/// <summary>
/// Ego-Kamera-Controller (First-Person-View). Sitzt als Kind von Player/HeadAnchor.
///
/// Architektur (Best Practice fuer FPS):
///   Player        -> Yaw   (Y-Rotation, also Koerper-Drehung)
///   HeadAnchor    -> Pitch (X-Rotation, also Kopf-Nicken)
///   Diese Camera  -> nur Position (relativ zum HeadAnchor)
///
/// Die zwei Achsen liegen bewusst auf zwei verschiedenen Knoten — das verhindert
/// Gimbal-Lock und entkoppelt Bewegungs-Forward (Player-Yaw) vom Blick-Pitch.
///
/// Phase 21 wird zusaetzlich einen Free-Look-Modus einfuehren, in dem nur der
/// HeadAnchor rotiert (Body bleibt stehen). Dieser Controller stellt dafuer
/// bereits den Hook <see cref="FreeLookHeld"/> zur Verfuegung.
/// </summary>
public partial class FirstPersonCameraController : Camera3D
{
    [Export] public float MouseSensitivity = 0.0022f;   // rad pro Mauspixel
    [Export] public float MaxPitchDeg = 85f;            // ±85° vermeidet Zenit-Artefakte
    [Export] public float KeyTurnSpeed = 1.8f;          // rad/s fuer Pfeiltasten als Maus-Backup

    /// <summary>
    /// Wird in Phase 21 von aussen gesetzt: solange true, geht Yaw NICHT in den Body,
    /// sondern in den HeadAnchor-Yaw-Offset. Phase 20 laesst dieses Flag immer false.
    /// </summary>
    public bool FreeLookHeld { get; set; }

    private Node3D _player = null!;       // Body-Yaw
    private Node3D _headAnchor = null!;   // Pitch (und in Phase 21: Free-Look-Yaw)

    private float _pitch;                 // aktueller Pitch in Radiant
    private bool _captured;               // Cursor-Status

    private float _maxPitchRad;

    public override void _Ready()
    {
        // Eingabe-Akkumulation deaktivieren — Best Practice fuer praezise Maus-Eingabe.
        // Sonst werden mehrere Mausbewegungen pro Frame zusammengefasst und das Bild ruckelt.
        Input.UseAccumulatedInput = false;

        // Camera -> HeadAnchor -> Player. Beide Vorfahren explizit aufloesen.
        _headAnchor = GetParent<Node3D>();
        _player = _headAnchor.GetParent<Node3D>();
        _maxPitchRad = Mathf.DegToRad(MaxPitchDeg);

        // Initialwerte aus aktuellem Transform ziehen, damit das erste Maus-Event
        // keinen sichtbaren Sprung verursacht.
        _pitch = _headAnchor.Rotation.X;
    }

    /// <summary>
    /// Aktiviert den FPS-Modus: Kamera wird Current, Cursor wird eingefangen.
    /// </summary>
    public void Activate()
    {
        MakeCurrent();
        CaptureMouse(true);
    }

    /// <summary>
    /// Verlaesst den FPS-Modus: Cursor wieder sichtbar machen. Die alte Kamera muss
    /// von aussen wieder Current geschaltet werden (siehe Main).
    /// </summary>
    public void Deactivate()
    {
        CaptureMouse(false);
    }

    private void CaptureMouse(bool capture)
    {
        _captured = capture;
        Input.MouseMode = capture ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
    }

    public override void _Process(double delta)
    {
        if (!Current) return;

        // Pfeiltasten als Maus-Backup (Schueler ohne Maus / Touchpad-User).
        float yawDelta = 0f;
        float pitchDelta = 0f;
        if (Input.IsPhysicalKeyPressed(Key.Left))  yawDelta   -= KeyTurnSpeed * (float)delta;
        if (Input.IsPhysicalKeyPressed(Key.Right)) yawDelta   += KeyTurnSpeed * (float)delta;
        if (Input.IsPhysicalKeyPressed(Key.Up))    pitchDelta -= KeyTurnSpeed * (float)delta;
        if (Input.IsPhysicalKeyPressed(Key.Down))  pitchDelta += KeyTurnSpeed * (float)delta;

        if (yawDelta != 0f || pitchDelta != 0f)
            ApplyLook(yawDelta, pitchDelta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Current) return;

        // Esc gibt den Cursor frei — wir verlassen den FPS-Modus aber NICHT;
        // den Modus-Switch macht Main per HUD-Toggle. Esc ist nur eine
        // Bequemlichkeit, damit man kurz auf das HUD klicken kann.
        if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            CaptureMouse(false);
            GetViewport().SetInputAsHandled();
            return;
        }

        // Klick ins Spiel-Bild faengt den Cursor wieder ein, falls er per Esc
        // freigegeben wurde.
        if (@event is InputEventMouseButton { Pressed: true } mb && !_captured)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                CaptureMouse(true);
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (@event is InputEventMouseMotion motion && _captured)
        {
            ApplyLook(motion.Relative.X * MouseSensitivity,
                      motion.Relative.Y * MouseSensitivity);
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// Wendet ein Yaw-/Pitch-Delta an. Yaw geht in Body (oder im Free-Look in HeadAnchor),
    /// Pitch immer in den HeadAnchor.
    /// </summary>
    private void ApplyLook(float yawDelta, float pitchDelta)
    {
        if (yawDelta != 0f)
        {
            // Phase 21: FreeLookHeld == true wuerde Yaw in HeadAnchor schreiben.
            // Phase 20: FreeLookHeld bleibt immer false, deshalb laeuft alles in den Body.
            Node3D yawTarget = FreeLookHeld ? _headAnchor : _player;
            // Negativ, weil positive Maus-X (rechts) "nach rechts schauen" bedeutet,
            // was in Godot's Y-up-Konvention einer NEGATIVEN Y-Rotation entspricht.
            yawTarget.RotateY(-yawDelta);
            // Drift nach vielen Drehungen verhindern.
            Transform3D t = yawTarget.Transform;
            t.Basis = t.Basis.Orthonormalized();
            yawTarget.Transform = t;
        }

        if (pitchDelta != 0f)
        {
            _pitch = Mathf.Clamp(_pitch + pitchDelta, -_maxPitchRad, _maxPitchRad);
            // Pitch wird absolut gesetzt (nicht inkrementell), damit Clamp greift.
            _headAnchor.Rotation = new Vector3(_pitch, _headAnchor.Rotation.Y, 0f);
        }
    }

    public override void _Notification(int what)
    {
        // Fenster-Fokus verloren -> Cursor freigeben, sonst klemmt er unsichtbar.
        if (what == NotificationApplicationFocusOut && _captured)
            CaptureMouse(false);
    }
}
