from DirectLineAPI import DirectLineAPI
from Unit import Unit
import requests
import json
from time import sleep

#test = Unit(False)

#test.run_all()

#test.scenario_8()
#test.print_failures()
"""
# test bot
dl = DirectLineAPI("gl3MVIwDHsE.cwA.FAc.X53ON0Ip0FhQuDTo9oc_KYg5zAqFlTcKM-luImFCXYw")
# main bot
#dl = DirectLineAPI("rw55zFN6KD0.89y0pZlqBXFQgRURrCfHGEwKtOSVBnJBvgbuoLNj8r0")
dl.start_conversation()
dl.send_message("English")
print(dl.get_message())
dl.send_message("Subjects")
print(dl.get_message())
dl.send_message("Start over")
print(dl.get_message())
dl.send_message("English")
print(dl.get_message())
"""

url = "https/testsloteurope.azurewebsites.net/443/api/ProactiveAPI"

dl = DirectLineAPI("gl3MVIwDHsE.-kP_nWcloI-0haYgaS03eqbJDYBU1KwaXmGyoUhIk8Q")

