import sys
from time import sleep

import requests
# from monitor import Monitor

MONITOR_EMAIL = "cbapimonitor@gmail.com"
MONITOR_PASSWORD = "Wilmslow1954*"
RECIPIENTS = ["jake.kavanagh@ucdconnect.ie", "daniel.jordan1@ucdconnect.ie"]


class DirectLineAPI(object):
    """Shared methods for the parsed result objects."""

    def __init__(self, direct_line_secret):
        self._direct_line_secret = direct_line_secret
        self._base_url = 'https://directline.botframework.com/v3/directline'
        self._set_headers()
        self._start_conversation()

    def _set_headers(self):
        headers = {'Content-Type': 'application/json'}
        value = ' '.join(['Bearer', self._direct_line_secret])
        headers.update({'Authorization': value})
        self._headers = headers

    def _start_conversation(self):
        # For Generating a token use
        # url = '/'.join([self._base_url, 'tokens/generate'])
        # botresponse = requests.post(url, headers=self._headers)
        # jsonresponse = botresponse.json()
        # self._token = jsonresponse['token']

        # Start conversation and get us a conversationId to use
        url = '/'.join([self._base_url, 'conversations'])
        botresponse = requests.post(url, headers=self._headers)

        # Extract the conversationID for sending messages to bot
        jsonresponse = botresponse.json()

        # self._conversationid = jsonresponse['conversationId']
        self._conversationid = jsonresponse['conversationId']

    def send_message(self, text):
        """Send raw text to bot framework using directline api"""
        url = '/'.join([self._base_url, 'conversations', self._conversationid, 'activities'])
        jsonpayload = {
            'conversationId': self._conversationid,
            'type': 'message',
            'from': {'id': 'user1'},
            'text': text
        }
        botresponse = requests.post(url, headers=self._headers, json=jsonpayload)
        if botresponse.status_code == 200:
            return "message sent"
        return "error contacting bot"

    def get_message(self):
        """Get a response message back from the botframework using directline api"""
        url = '/'.join([self._base_url, 'conversations', self._conversationid, 'activities'])
        botresponse = requests.get(url, headers=self._headers,
                                   json={'conversationId': self._conversationid})
        if botresponse.status_code == 200:
            jsonresponse = botresponse.json()
            #print(jsonresponse["activities"])
            responses = [jsonresponse['activities'][i]['text'] for i in range(0, len(jsonresponse['activities']))]
            return responses

        return "error contacting bot for response"



def send_message(bot, text):
    message = text
    bot.send_message(message)
    bot_response = bot.get_message()
    if message in bot_response:
        bot_response.remove(message)
    return bot_response

def main():
    hakeem = "Oa7Cb-s5mxw.cwA.-HY.0BsfYxQE-gMpFVg6pI80oRkHVbXmRWnCEd3TVWAywh4"
    test_bot = 'gl3MVIwDHsE.cwA.JFM.R0Lr5xn0xzSCh9jcCDkRcN8Tz9TUwJpyJyuQuGOU7Mw'
    hakeem = DirectLineAPI(hakeem)
    test_bot = DirectLineAPI(test_bot)
    expected_answer = ["En", 
                              "مرحبا! أنا حكيم ، رفيق التعليم الافتراضي الخاص بك. سيكون من المثير أن نساعدك في اكتشاف وتعلم أشياء جديدة.",
                              "Hi! I'm Hakeem, your virtual Learning Companion. It will be exciting to help you discover and learn new things.",
                              "اسألني عن المساعدة في أي وقت للحصول على قائمة بالأشياء التي يمكنني القيام بها",
                              "Ask me for help at any time for a list of things that I can do ",
                              "رد باستخدام اللغة التي تريد استخدامها..",
                              "Reply with the language you want me to use."
                              ]

    first_message = "hello"
    second_message = "start_over"
    bot_response = send_message(hakeem, first_message)
    bot_response = send_message(hakeem, second_message)

    for i in bot_response:
        print(i)

    for i in bot_response:
        if i in expected_answer:

            break


if __name__ == "__main__":
    main()