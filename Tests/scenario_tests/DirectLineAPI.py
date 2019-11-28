import requests

class DirectLineAPI(object):
    """Shared methods for the parsed result objects."""

    def __init__(self, direct_line_secret):
        self._direct_line_secret = direct_line_secret
        self._base_url = 'https://directline.botframework.com/v3/directline'
        self._set_headers()
        #self._start_conversation()

    def _set_headers(self):
        headers = {'Content-Type': 'application/json'}
        value = ' '.join(['Bearer', self._direct_line_secret])
        headers.update({'Authorization': value})
        self._headers = headers

    def start_conversation(self):
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
        #print(botresponse)
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
        timeout = 5
        count = 0
        botresponse = requests.post(url, headers=self._headers, json=jsonpayload)
        # direct line seems to not work perfectly so try to send the message a number of times
        # and if the message doesn't send properly once then we can conclude it is not working
        # this will have to do until we can figure out why the direct line sucks
        while botresponse.status_code != 200 and count < timeout:
            print(botresponse.status_code)
            botresponse = requests.post(url, headers=self._headers, json=jsonpayload)
            count += 1
        if botresponse.status_code == 200:

            return "Message sent"
        print(botresponse.status_code)
        return "error contacting bot"

    def get_message(self, count):
        """Get a response message back from the botframework using directline api"""
        url = '/'.join([self._base_url, 'conversations', self._conversationid, 'activities'])
        botresponse = requests.get(url, headers=self._headers,
                                   json={'conversationId': self._conversationid})
        if botresponse.status_code == 200:
            jsonresponse = botresponse.json()
            #print(jsonresponse["activities"])
            response = jsonresponse["activities"][len(jsonresponse["activities"])-1]["text"]
            if response == "":
                # then the last message was a set of buttons
                response = jsonresponse["activities"][len(jsonresponse["activities"]) - 2]["text"]

            responses = list()

            #responses = [jsonresponse['activities'][i]['text'] for i in range(0, len(jsonresponse['activities']))]

            for i in jsonresponse['activities']:
                if i["from"]["id"] != "user1" and len(i["attachments"]) == 0:
                    #print(i)
                    responses.append(i["text"])
                elif i["from"]["id"] == "user1":
                    responses.append(i["text"])
            return responses

        print(botresponse.status_code)
        return "error contacting bot for response"

    def get_conversation(self, count):

        reply = self.get_message(count)
        conversation = list()

        for i, string in enumerate(reply):

            conversation.append(string)

        return conversation