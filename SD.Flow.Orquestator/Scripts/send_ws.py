import pywhatkit
import sys
import time
# Estas son las que necesitan el paquete pynput
from pynput.keyboard import Key, Controller

keyboard = Controller()

def enviar_mensaje(telefono, mensaje):
    try:
        # Sintaxis: telefono (con +), mensaje, hora, minuto
        # Usamos 'sendwhatmsg_instantly' para que sea inmediato
        pywhatkit.sendwhatmsg_instantly(
            phone_no=telefono, 
            message=mensaje,
            wait_time=15, # Segundos que espera a que cargue WhatsApp Web
            tab_close=True # Cierra la pestaña al terminar
        )
        
        # Pequeño truco: a veces hay que presionar 'Enter' manualmente
        time.sleep(2)
        keyboard.press(Key.enter)
        keyboard.release(Key.enter)
        
        print(f"Mensaje enviado a {telefono}")
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    # Capturamos los argumentos desde .NET
    # Ejemplo: python send_ws.py +584121234567 "Hola desde .NET"
    if len(sys.argv) > 2:
        numero = sys.argv[1]
        texto = sys.argv[2]
        enviar_mensaje(numero, texto)
    else:
        print("Faltan argumentos: numero y mensaje")