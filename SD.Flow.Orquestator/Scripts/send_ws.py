import sys
import time

def enviar_mensaje(telefono: str, mensaje: str, dry_run: bool = False) -> None:
    if dry_run:
        print(f"[DRY-RUN] Simulación de envío a {telefono}: {mensaje[:80]}{'...' if len(mensaje) > 80 else ''}")
        return

    import pywhatkit
    from pynput.keyboard import Key, Controller

    keyboard = Controller()

    pywhatkit.sendwhatmsg_instantly(
        phone_no=telefono,
        message=mensaje,
        wait_time=15,
        tab_close=True,
    )

    time.sleep(2)
    keyboard.press(Key.enter)
    keyboard.release(Key.enter)

    print(f"Mensaje enviado a {telefono}")


def main() -> int:
    args = sys.argv[1:]
    dry_run = False

    if not args:
        print("Uso: python send_ws.py [--dry-run] <telefono> <mensaje>", file=sys.stderr)
        return 1

    if args[0] == "--dry-run":
        dry_run = True
        args = args[1:]

    if len(args) < 2:
        print("Faltan argumentos: número de teléfono y mensaje.", file=sys.stderr)
        return 1

    telefono = args[0].strip()
    mensaje = " ".join(args[1:])

    if not telefono.startswith("+"):
        print("El teléfono debe incluir código de país (ej: +584121234567).", file=sys.stderr)
        return 1

    try:
        enviar_mensaje(telefono, mensaje, dry_run=dry_run)
        return 0
    except ModuleNotFoundError as e:
        print(
            f"Dependencia faltante: {e.name}. Ejecute: pip install -r Scripts/requirements.txt",
            file=sys.stderr,
        )
        return 2
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
