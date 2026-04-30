using Godot;
using Maze.Model;

namespace Maze.Views;

/// <summary>
/// Frei steuerbare 3D-Kamera fuer die Maze-Ansicht.
/// Bewegung: WASD horizontal in Blickrichtung, QE vertikal in Welt-Y,
/// Shift verdoppelt die Geschwindigkeit. Drehung: RMB + Maus oder Pfeiltasten.
/// Zoom: Mausrad als Dolly entlang der Blickrichtung.
/// </summary>
public partial class CameraController3D : Camera3D
{
    [Export] public float MoveSpeed = 8f;
    [Export] public float SprintMultiplier = 2f;
    [Export] public float MouseSensitivity = 0.003f;
    [Export] public float KeyTurnSpeed = 1.5f;        // rad/s fuer Pfeiltasten
    [Export] public float ZoomStep = 1.5f;
    [Export] public float ZoomSprintMultiplier = 3f;
    [Export] public float FollowDistance = 4.5f;     // Welt-Einheiten hinter dem Target
    [Export] public float FollowHeight = 3.0f;       // Welt-Einheiten ueber dem Target
    [Export] public float FollowSmoothing = 6.0f;    // hoeher = schnellere Annaeherung

    // Yaw/Pitch werden separat gefuehrt, damit die Rotation immer aus
    // Basis.FromEuler aufgebaut wird (kein Gimbal-Drift).
    private float _yaw;
    private float _pitch;
    private bool _mouseLook;

    private Node3D _followTarget;
    public bool FollowMode { get; private set; }

    // Orbit-Zustand fuer den Follow-Modus: sphärische Koordinaten um das Target.
    private float _followOrbitYaw;    // horizontaler Winkel (Bogen links/rechts)
    private float _followOrbitPitch;  // vertikaler Winkel (Bogen rauf/runter)
    private float _followOrbitRadius; // Abstand zum Target in Welt-Einheiten

    public override void _Ready()
    {
        // Initialwerte aus dem aktuellen Transform ziehen, damit auch ohne FitToMaze
        // ein konsistenter Startzustand existiert.
        Vector3 euler = Basis.GetEuler();
        _pitch = euler.X;
        _yaw = euler.Y;
    }

    public override void _Process(double delta)
    {
        if (FollowMode && _followTarget != null)
        {
            UpdateFollowCamera(delta);
            return;
        }

        HandleMovement(delta);
        HandleKeyboardLook(delta);
        ApplyRotation();
    }

    private void UpdateFollowCamera(double delta)
    {
        Vector3 targetPos = _followTarget.GlobalPosition;

        // Sphärische Koordinaten: Orbit-Position aus Radius, Yaw und Pitch berechnen.
        // Pitch = 0 wäre Äquator (Horizont), Pi/2 wäre senkrecht von oben.
        float cosP = Mathf.Cos(_followOrbitPitch);
        float sinP = Mathf.Sin(_followOrbitPitch);
        Vector3 orbitOffset = new Vector3(
            Mathf.Sin(_followOrbitYaw) * cosP,
            sinP,
            Mathf.Cos(_followOrbitYaw) * cosP
        ) * _followOrbitRadius;

        Vector3 desiredPos = targetPos + orbitOffset;

        // Smooth lerp: Annaeherungsgeschwindigkeit haengt von delta UND dem
        // Smoothing-Faktor ab; je weiter weg, desto schneller wird aufgeholt.
        float lerpFactor = 1f - Mathf.Exp(-FollowSmoothing * (float)delta);
        GlobalPosition = GlobalPosition.Lerp(desiredPos, lerpFactor);

        // Zum Target-Mittelpunkt blicken, leicht nach oben korrigiert.
        LookAt(targetPos + new Vector3(0, 0.3f, 0), Vector3.Up);

        // Yaw/Pitch synchron halten, damit der Wechsel zurueck in den Free-Modus
        // direkt an der aktuellen Blickrichtung weitermacht statt zurueckzuspringen.
        Vector3 euler = Basis.GetEuler();
        _pitch = euler.X;
        _yaw = euler.Y;
    }

    // Hinweis: Vector3.Forward ist in Godot (0, 0, -1), deshalb erhoeht
    // W -> Vector3.Forward die lokale -Z-Position via Translate korrekt = vorwaerts.
    private void HandleMovement(double delta)
    {
        // Eingabe als 3D-Richtungsvektor aufbauen:
        //   Vorwaerts/zurueck (W/S)  -> lokale -Z/+Z (forward/back)
        //   Seitwaerts (A/D)         -> lokale -X/+X (left/right)
        //   Vertikal (Q/E)            -> Welt-Y (E hoch, Q runter)
        Vector3 input = Vector3.Zero;
        if (Input.IsPhysicalKeyPressed(Key.W)) input += Vector3.Forward;
        if (Input.IsPhysicalKeyPressed(Key.S)) input += Vector3.Back;
        if (Input.IsPhysicalKeyPressed(Key.A)) input += Vector3.Left;
        if (Input.IsPhysicalKeyPressed(Key.D)) input += Vector3.Right;

        // QE bewegen sich entlang Welt-Y, unabhaengig vom Pitch der Kamera.
        Vector3 worldVertical = Vector3.Zero;
        if (Input.IsPhysicalKeyPressed(Key.E)) worldVertical += Vector3.Up;
        if (Input.IsPhysicalKeyPressed(Key.Q)) worldVertical += Vector3.Down;

        if (input == Vector3.Zero && worldVertical == Vector3.Zero)
            return;

        float speed = MoveSpeed;
        if (Input.IsPhysicalKeyPressed(Key.Shift))
            speed *= SprintMultiplier;

        // Horizontaler Anteil: in Kamera-Lokalkoordinaten transformieren.
        if (input != Vector3.Zero)
        {
            input = input.Normalized();
            Translate(input * speed * (float)delta);
        }

        // Vertikaler Anteil: direkt in Weltkoordinaten addieren.
        if (worldVertical != Vector3.Zero)
            Position += worldVertical.Normalized() * speed * (float)delta;
    }

    private void HandleKeyboardLook(double delta)
    {
        // Pfeiltasten als Maus-Backup. Funktioniert immer, auch ohne RMB.
        float yawDelta = 0f;
        float pitchDelta = 0f;
        if (Input.IsPhysicalKeyPressed(Key.Left))  yawDelta   += KeyTurnSpeed * (float)delta;
        if (Input.IsPhysicalKeyPressed(Key.Right)) yawDelta   -= KeyTurnSpeed * (float)delta;
        if (Input.IsPhysicalKeyPressed(Key.Up))    pitchDelta += KeyTurnSpeed * (float)delta;
        if (Input.IsPhysicalKeyPressed(Key.Down))  pitchDelta -= KeyTurnSpeed * (float)delta;
        _yaw += yawDelta;
        _pitch = Mathf.Clamp(_pitch + pitchDelta, -1.4f, 1.4f);
    }

    private void ApplyRotation()
    {
        // Rotation immer komplett aus Yaw/Pitch aufbauen, nicht inkrementell.
        Basis = Basis.FromEuler(new Vector3(_pitch, _yaw, 0));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Nur eingehende Events verarbeiten, wenn diese Kamera die aktuelle ist.
        // Wenn die 2D-View aktiv ist, sollen deren Input-Events durchlaufen.
        if (!Current)
            return;

        // Im Follow-Modus: nur Zoom (Mausrad) und Orbit (RMB + Maus) erlaubt.
        if (FollowMode)
        {
            HandleFollowInput(@event);
            return;
        }

        // RMB druecken/loslassen schaltet Mouse-Look an/aus.
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Right)
            {
                _mouseLook = mb.Pressed;
                Input.MouseMode = mb.Pressed ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
                GetTree().Root.SetInputAsHandled();
                return;
            }

            // Mausrad als Dolly: bewegt die Kamera entlang der lokalen Forward-Achse.
            if (mb.Pressed && (mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelDown))
            {
                float step = ZoomStep;
                if (Input.IsPhysicalKeyPressed(Key.Shift))
                    step *= ZoomSprintMultiplier;

                // WheelUp = naeher heran (vorwaerts), WheelDown = weiter weg (rueckwaerts).
                Vector3 direction = mb.ButtonIndex == MouseButton.WheelUp ? Vector3.Forward : Vector3.Back;
                Translate(direction * step);
                GetTree().Root.SetInputAsHandled();
                return;
            }
        }

        // Maus-Bewegung im Look-Modus aendert Yaw/Pitch.
        if (@event is InputEventMouseMotion motion && _mouseLook)
        {
            _yaw -= motion.Relative.X * MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * MouseSensitivity, -1.4f, 1.4f);
            GetTree().Root.SetInputAsHandled();
        }
    }

    public override void _Notification(int what)
    {
        // Wenn das Fenster den Fokus verliert (Alt-Tab), den Cursor freigeben,
        // sonst klemmt die Maus unsichtbar im Spielbereich.
        if (what == NotificationApplicationFocusOut && _mouseLook)
        {
            _mouseLook = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public void FitToMaze(Model.Maze maze)
    {
        float w = maze.Width;
        float h = maze.Height;
        float centerX = w / 2f;
        float centerZ = h / 2f;
        float height = Mathf.Max(w, h) * 0.8f;

        Position = new Vector3(centerX, height, centerZ + height * 0.7f);
        LookAt(new Vector3(centerX, 0, centerZ), Vector3.Up);

        // Yaw/Pitch aus dem fertigen Look-At zurueckrechnen, damit die WASD-Steuerung
        // direkt vom Auto-Fit-Zustand uebernimmt - sonst wuerde der erste Tastendruck
        // die Kamera zurueck in den alten Winkel reissen.
        Vector3 euler = Basis.GetEuler();
        _pitch = euler.X;
        _yaw = euler.Y;
    }

    /// <summary>
    /// Aktiviert den Verfolger-Modus mit dem uebergebenen Target. Solange der Modus
    /// aktiv ist, ignoriert die Kamera WASD/QE/Pfeiltasten/Maus und folgt stattdessen
    /// dem Target weich aus halbhoher Schraege von hinten.
    /// </summary>
    private void HandleFollowInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            // RMB schaltet Orbit-Look an/aus.
            if (mb.ButtonIndex == MouseButton.Right)
            {
                _mouseLook = mb.Pressed;
                Input.MouseMode = mb.Pressed ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
                GetTree().Root.SetInputAsHandled();
                return;
            }

            // Mausrad aendert den Orbit-Radius (Zoom).
            if (mb.Pressed && (mb.ButtonIndex == MouseButton.WheelUp || mb.ButtonIndex == MouseButton.WheelDown))
            {
                float step = ZoomStep;
                if (Input.IsPhysicalKeyPressed(Key.Shift))
                    step *= ZoomSprintMultiplier;
                // WheelUp = naeher heran, WheelDown = weiter weg.
                _followOrbitRadius = Mathf.Clamp(
                    _followOrbitRadius + (mb.ButtonIndex == MouseButton.WheelUp ? -step : step),
                    1f, 200f);
                GetTree().Root.SetInputAsHandled();
            }
        }

        // Maus-Bewegung im Orbit-Look-Modus dreht die Kamera um das Target.
        if (@event is InputEventMouseMotion motion && _mouseLook)
        {
            _followOrbitYaw   -= motion.Relative.X * MouseSensitivity;
            // Pitch zwischen knapp ueber dem Boden (0.05 rad) und fast senkrecht (Pi/2 - 0.05).
            _followOrbitPitch  = Mathf.Clamp(
                _followOrbitPitch - motion.Relative.Y * MouseSensitivity,
                0.05f, Mathf.Pi / 2f - 0.05f);
            GetTree().Root.SetInputAsHandled();
        }
    }

    public void EnableFollow(Node3D target)
    {
        _followTarget = target;
        FollowMode = true;

        // Orbit-Radius und -Winkel aus den Export-Feldern initialisieren.
        _followOrbitRadius = Mathf.Sqrt(FollowHeight * FollowHeight + FollowDistance * FollowDistance);
        _followOrbitPitch  = Mathf.Atan2(FollowHeight, FollowDistance);
        _followOrbitYaw    = 0f;

        // Maus-Look sicher abschalten, damit der Cursor nicht im Spielbereich klemmt.
        if (_mouseLook)
        {
            _mouseLook = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }

    public void DisableFollow()
    {
        _followTarget = null;
        FollowMode = false;
        
        // Zustand vollständig zurücksetzen fuer Free-Mode
        if (_mouseLook)
        {
            _mouseLook = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }
}
