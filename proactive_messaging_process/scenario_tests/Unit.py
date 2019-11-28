from . import deployed_test
import requests
import json


class Unit():
    """
    This class handles the unit testing of the bot
    To add a test, create a new method and add it's execution
    to the 'run_all' method. All test can then be run
    by executing the main.py script
    """

    hakeem = ""

    def __init__(self):

        self.hakeem = "Oa7Cb-s5mxw.cwA.-HY.0BsfYxQE-gMpFVg6pI80oRkHVbXmRWnCEd3TVWAywh4" #Not sure if this key is valid anymore
        self.hakeem = deployed_test.DirectLineAPI(self.hakeem)

    def start_over(self):
        self.hakeem.send_message("start over")
        self.hakeem.send_message("en")
        self.hakeem.send_message("Mariam")

    def basic(self):
        """
        Test for basic bot response
        """
        print("BASIC TEST")

        self.hakeem.send_message("hello")
        response = self.hakeem.get_message()

        if len(response) > 0:
            print("Success")

        else:

            print("Failure")

        print("------------------")

    def welcome(self):
        """
        test for correct welcome message
        NB. Must be run in the same conversation as the basic test
        """
        print("WELCOME MESSAGE TEST")

        expected = [
            "مرحبا! أنا حكيم ، رفيق التعليم الافتراضي الخاص بك. سيكون من المثير أن نساعدك في اكتشاف وتعلم أشياء جديدة.",
            "Hi! I'm Hakeem, your virtual Learning Companion. It will be exciting to help you discover and learn new things.",
            "اسألني عن المساعدة في أي وقت للحصول على قائمة بالأشياء التي يمكنني القيام بها :)",
            "Ask me for help at any time for a list of things that I can do :)",
            "رد باستخدام اللغة التي تريد استخدامها..",
            "Reply with the language you want me to use."]

        self.hakeem.send_message("start over")
        response = self.hakeem.get_message()

        start = response.index("start over")
        count = 0

        try:
            for i in range(start + 1, len(response)):

                if response[i] != expected[count]:
                    print("Failure")
                    print("response", response[i])
                    print("expected", expected[count])
                    break

                count += 1
                if count == len(expected):
                    print("Success")
                    break

        except:

            print("Failure")

        print("------------------")

    def luis_learning(self):

        """
        Test that the luis learning intent returns the intended response
        from the bot and then subsequently test an entire branch of the tree
        """

        print("SCENARIO 1")

        #for some reason the bot can't receive the message below through direct line
        #sending a second message will result in the bot defaulting to
        #"What would you like to learn?"
        self.hakeem.send_message("en")
        self.hakeem.send_message("Mariam")
        self.hakeem.send_message("I want to learn business")
        self.hakeem.send_message("Starting a Business")

        response = self.hakeem.get_message()
        expected = ["For starting a business, I currently know about one course:\n\n[Starting a Small Business](https://hplife.edcastcloud.com/learn/starting-a-small-business-open?locale=en)"]

        start = response.index("Starting a Business")
        count = 0

        try:
            for i in range(start+1, len(response)):
                if expected[count] != response[i]:
                    print("Failure")
                    print("response", response[i])
                    print("expected", expected[count])
                    break

                count += 1
                if count == len(expected):
                    print("Success")
                    break
        except:
            print("Failure")

        print("------------------")

    def luis_QnA(self):

        """
        Check that QnA intent of luis return correctly
        """

        print("Luis_intent Question")
        #below insert the LUISENDPOINT from Web.config - remember to delete it after so it doesn't get committed to GitHub
        endpoint = "********&verbose=true&timezoneOffset=60&q=who&are&you"
        QnA_response = requests.get(endpoint)
        json = QnA_response.json()
        if json["topScoringIntent"]["intent"] == "Question":

            print("Success")

        else:

            print("Failure")
            print("Expected Intent: Question")
            print("Intent Recognised:", json["topScoringIntent"]["intent"])

        print("------------------")

    def luis_Suggestion(self):

        """
        Check Luis intent suggestion works as intended
        """

        print("LUIS_INTENT SUGGESTION")
        self.start_over()
        self.hakeem.send_message("what should i learn")
        expected = "Would you like to see my full list of subjects?"
        response = self.hakeem.get_message()
        start = response.index("what should i learn")
        if response[start+1] == expected:
            print("Success")

        else:
            print("Failure")
            print("Expected:", expected)
            print("Response:", response[start+1])

        print("------------------")


    def change_language(self):

        """
        Check bot command to change current language works as intended
        """

        print("CHANGE LANGUAGE")
        self.start_over()
        self.hakeem.send_message("use arabic")
        response = self.hakeem.get_message()
        start = response.index("use arabic")
        expected = "حسناً ، سأتحدث إليك باللغة العربية من الآن فصاعداً"

        try:
            if response[start + 1] != expected:
                print("Failure")
            else:
                print("Success")

        except:
            print("Failure")

        print("------------------")


    def QnA_test(self):

        """
        Check the QnA maker service is running as intended by
        sending a direct query to it and comparning the response
        to the expected response
        """

        print("QNA_TEST")
         #These details are in the Web.config file. You can look at the QnA files in NetHope subfolder to see usage. Remember to delete so as not
        #to commit details to GitHub
        endpoint_host = ""
        endpoint_key = ""
        kbid = ""
        key = ""

        uri = endpoint_host + "/" + "/qnamaker/knowledgebases/" + kbid + "/generateAnswer"

        header = {'Authorization': "EndpointKey " + endpoint_key, 'content-type' : "application/json"}
        response = requests.post(uri, data=json.dumps({"question": "whats your favourite colour"}), headers=header)

        try:
            answer = response.json()

            if answer['answers'][0]['answer'].strip() != "I like beige":

                print("Failure")
                print("Expected:", "I like beige")
                print("Response:", answer['answers'][0]['answer'].strip())

            else:

                print("Success")

        except:
            print("Failure")

        print("------------------")


    def run_all(self):

        self.basic()
        self.welcome()
        self.luis_learning()
        self.luis_QnA()
        self.luis_Suggestion()
        self.change_language()
        self.QnA_test()


    def run_test(self):

        self.QnA_test()



