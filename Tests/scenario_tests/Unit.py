from DirectLineAPI import *
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
    failures = dict()
    passed = 0
    total = 0

    def __init__(self, main):

        self.main = main
        if main:
            #These two self.hakeem are expired find new on in Web.config but remember to remove before commiting to GitHub
            self.hakeem = "rw55zFN6KD0.16CZgmfntCWNwINAetA2iSAgtdPR00dKak-tuGyPl-0"

        else:

            self.hakeem = "gl3MVIwDHsE.-kP_nWcloI-0haYgaS03eqbJDYBU1KwaXmGyoUhIk8Q"

        self.hakeem = DirectLineAPI(self.hakeem)
        self.hakeem.start_conversation()

    def restart(self):
        self.hakeem.send_message("hello")
        self.hakeem.send_message("start over")

    def basic(self):
        """
        Test that bot is responding
        """
        print("BASIC TEST")
        #self.hakeem.start_conversation()
        self.restart()
        self.hakeem.send_message("hello")
        response = self.hakeem.get_message(1)

        if len(response) > 0:
            print("Success")
            self.passed += 1

        else:

            print("Failure")

        self.total += 1

        print("------------------")

    def luis_QnA(self):

        """
        Check that QnA intent of luis return correctly
        """

        print("LUIS INTENT QUESTION")
        #below insert the LUISENDPOINT from Web.config - remember to delete it after so it doesn't get committed to GitHub
        endpoint = "********&verbose=true&timezoneOffset=60e&q=who&are&you"
        QnA_response = requests.get(endpoint)
        if QnA_response.status_code == 200:
            json = QnA_response.json()
        else:
            print("Failed to connect to Luis")
            return
        if json["topScoringIntent"]["intent"].lower() == "question":

            print("Success")
            self.passed += 1

        else:

            print("Failure")
            print("Expected Intent: Question")
            print("Intent Recognised:", json["topScoringIntent"]["intent"])

        self.total += 1
        print("------------------")

    def luis_Suggestion(self):

        """
        Check Luis intent suggestion works as intended
        """

        print("LUIS INTENT SUGGESTION")
        #below insert the LUISENDPOINT from Web.config - remember to delete it after so it doesn't get committed to GitHub
        endpoint = "******&verbose=true&timezoneOffset=60&q=what&should&i&learn"
        QnA_response = requests.get(endpoint)
        if QnA_response.status_code == 200:
            json = QnA_response.json()
        else:
            print("Failed to connect to Luis")
            return

        if json["topScoringIntent"]["intent"].lower() == "suggestion":

            self.passed += 1
            print("Success")

        else:

            print("Failure")
            print("Expected Intent: Suggestion")
            print("Intent Recognised:", json["topScoringIntent"]["intent"])

        self.total += 1
        print("------------------")


    def luis_Sentiment(self):

        """
        Check Luis intent suggestion works as intended
        """

        print("LUIS INTENT SENTIMENT")
        #below insert the LUISENDPOINT from Web.config - remember to delete it after so it doesn't get committed to GitHub
        endpoint = "******&verbose=true&timezoneOffset=60&q=good&thankyou"
        QnA_response = requests.get(endpoint)
        if QnA_response.status_code == 200:
            json = QnA_response.json()
        else:
            print("Failed to connect to Luis")
            return

        if json["sentimentAnalysis"]["label"].lower() == "positive":

            print("Success")
            self.passed += 1

        else:

            print("Failure")
            print("Expected Sentiment: Positive")
            print("Sentiment Recognised: Negative")

        print("------------------")
        self.total += 1

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

                self.passed += 1
                print("Success")

        except:
            print("Failure")

        self.total += 1
        print("------------------")


    def scenario_1(self):

        print("SCENARIO 1")
        self.hakeem.start_conversation()
        self.restart()
        #expected = "Here's what I currently know about. Respond with the subject and if needed I'll ask for more guidance."
        expected = "Now let’s find something to learn!"
        self.restart()
        self.hakeem.send_message("English")
        self.hakeem.send_message("good thanks")
        self.hakeem.send_message("Continue")
        message_count = 3

        reply = self.hakeem.get_message(message_count)

        # get index of reply directly after the last message
        index = len(reply) - (reply[::-1].index("Continue"))

        reply = reply[index]

        self.string_eq(expected, reply, "scenario_1", message_count)

        print("------------------")

    def scenario_2(self):

        print("SCENARIO 2")
        self.hakeem.start_conversation()

        self.restart()
        self.hakeem.send_message("English")
        self.hakeem.send_message("Good thanks")
        self.hakeem.send_message("continue")
        self.hakeem.send_message("Start Over")
        self.hakeem.send_message("English")
        self.hakeem.send_message("Good")
        message_count = 6

        expected = "Glad to hear it!"

        reply = self.hakeem.get_message(message_count)

        index = len(reply) - (reply[::-1].index("Good"))

        reply = reply[index]

        self.string_eq(expected, reply, "scenario_2", message_count)

        print("------------------")

    def scenario_3(self):

        print("SCENARIO 3")
        self.hakeem.start_conversation()

        self.restart()
        self.hakeem.send_message("English")
        self.hakeem.send_message("Good thanks")
        self.hakeem.send_message("continue")
        self.hakeem.send_message("Business ")
        self.hakeem.send_message("Starting a Business")
        self.hakeem.send_message("Planning your Personal Finances")
        self.hakeem.send_message("Learn More")
        self.hakeem.send_message("Take Course")
        self.hakeem.send_message("Go Back")
        self.hakeem.send_message("Go Back")
        message_count = 10

        expected = "Great selection!\n\nOvercome your financial problems by learning to control your finances and savings regardless of the amount of money you make every month"

        reply = self.hakeem.get_message(message_count)
        print(reply)
        index = len(reply) - (reply[::-1].index("Go Back"))

        reply = reply[index]

        self.string_eq(expected, reply, "scenario_3", message_count)

        print("------------------")

    def scenario_4(self):

        print("SCENARIO 4")
        self.hakeem.start_conversation()

        self.restart()
        self.hakeem.send_message("English")
        self.hakeem.send_message("good")
        self.hakeem.send_message("continue")
        self.hakeem.send_message("Subjects")
        self.hakeem.send_message("Technology")
        self.hakeem.send_message("Programming")
        self.hakeem.send_message("Java Programming 1")
        self.hakeem.send_message("Learn More")
        self.hakeem.send_message("Go Back")
        self.hakeem.send_message("Go Back")
        message_count = 10

        expected = "Ah so you're interested in making programs and bots just like me! Programming is a very useful skill to have."

        reply = self.hakeem.get_message(message_count)
        print(reply)
        index = len(reply) - (reply[::-1].index("Go Back"))

        reply = reply[index]

        self.string_eq(expected, reply, "scenario_4", message_count)

        print("------------------")



    def about_hakeem(self):
        print("ABOUT HAKEEM")
        self.hakeem.start_conversation()

        self.restart()
        self.hakeem.send_message("English")
        self.hakeem.send_message("good")
        self.hakeem.send_message("About")
        message_count = 3
        expected = "I am glad you are curious and want to know more about me. That is a great learning skill to have. 😊 There are many quality skills-training, academic courses, and other learning opportunities available, but they can be hard to find. As a virtual Learning Companion, I use curated information from subject matter experts to help you find quality learning opportunities."

        reply = self.hakeem.get_message(message_count)
        index = len(reply) - (reply[::-1].index("About"))

        reply = reply[index]

        self.string_eq(expected, reply, "about_hakeem", message_count)

        print("------------------")


    def scenario_5(self):
        print("SCENARIO 5")
        self.hakeem.start_conversation()

        self.restart()
        self.hakeem.send_message("English")
        self.hakeem.send_message("good")
        self.hakeem.send_message("Continue")
        self.hakeem.send_message("Start Over")
        self.hakeem.send_message("English")
        self.hakeem.send_message("Good")
        self.hakeem.send_message("I want to learn technology")
        message_count = 7
        expected = "Good choice!\n\nWithin the subject of Technology there are specific subject areas you can focus on:"

        reply = self.hakeem.get_message(message_count)
        index = len(reply) - (reply[::-1].index("I want to learn technology"))

        reply = reply[index]

        self.string_eq(expected, reply, "scenario_5", message_count)

        print("------------------")


    def scenario_6(self):

        print("SCENARIO 6")
        self.hakeem.start_conversation()

        self.restart()
        self.hakeem.send_message("English")
        self.hakeem.send_message("good")
        self.hakeem.send_message("Continue")
        self.hakeem.send_message("Subjects")
        self.hakeem.send_message("Life Skills")
        self.hakeem.send_message("Personal Development")
        self.hakeem.send_message("The Science of Everyday Thinking ")
        self.hakeem.send_message("Take Course")
        message_count = 8
        expected = "The course The Science of Everyday Thinking can be found at the following link:"

        reply = self.hakeem.get_message(message_count)
        index = len(reply) - (reply[::-1].index("Take Course"))

        reply = reply[index]

        self.string_eq(expected, reply, "scenario_6", message_count)

        print("------------------")

    def scenario_7(self):

        print("SCENARIO 7")
        self.hakeem.start_conversation()

        self.restart()
        self.hakeem.send_message("عربى")
        self.hakeem.send_message("حسن")
        self.hakeem.send_message("مواصله")
        self.hakeem.send_message("topics")
        self.hakeem.send_message("See Subjects  ")
        self.hakeem.send_message("Language")
        self.hakeem.send_message("تعديل التفضيلات")
        self.hakeem.send_message("Edit Notification")
        self.hakeem.send_message("20")
        message_count = 9

        expected = "سوف تعرف تلقي الإخطارات كل 20 يوما"

        reply = self.hakeem.get_message(message_count)
        index = len(reply) - (reply[::-1].index("20"))

        reply = reply[index]

        self.string_eq(expected, reply, "scenario_7", message_count)

        print("------------------")

    def scenario_8(self):

        print("SCENARIO 8")
        self.hakeem.start_conversation()
        self.restart()
        self.hakeem.send_message("عربى")
        self.hakeem.send_message("حسن")
        self.hakeem.send_message("مواصله")
        self.hakeem.send_message("أريد ان تعلم الاعمال")
        message_count = 4
        expected = "اختيار جيد!  اطار موضوع \n\nالتكنولوجيا هناك مجالات محدده يمكنك التركيز عليها"

        reply = self.hakeem.get_message(message_count)
        index = len(reply) - (reply[::-1].index("أريد ان تعلم الاعمال"))

        reply = reply[index]

        self.string_eq(expected, reply, "scenario_8", message_count)

        print("------------------")



    def string_eq(self, expected, reply, caller, count):

        if expected.replace("\n", "").replace(" ", "") != reply.strip().replace("\n", "").replace(" ", ""):
            print("Failure")
            print("Expected: {0}".format(expected))
            print("Got: {0}".format(reply))
            conversation = self.hakeem.get_conversation(count)
            self.failures[caller] = conversation

        else:

            self.passed += 1
            print("Success")

        self.total += 1


    def print_failures(self):

        keys = self.failures.keys()
        x = 20
        for i in keys:
            print()
            print("=" * x),
            y = len(i)
            y = x - y
            y1 = y//2-1
            y2 = y1//2-1
            print("="*y1, end=""),
            print(" "*y2, end=""),
            print(i, end="")
            print(" "*y2, end=""),
            print("="*y1),
            print("=" * x),
            print()
            for j in self.failures[i]:
                print(j)
            print()


    def run_all(self):

        self.basic()
        self.luis_QnA()
        self.luis_Suggestion()
        self.luis_Sentiment()
        self.QnA_test()
        self.scenario_1()
        self.scenario_2()
        self.scenario_3()
        self.scenario_4()
        self.scenario_5()
        self.scenario_6()
        self.scenario_7()
        self.scenario_8()
        self.about_hakeem()

        print("\n**Passed {0} out of {1} tests**\n".format(self.passed, self.total))

        if len(self.failures.keys()) > 0:
            y = input("Would you like to view the full transcript for all failed tests? (y/n)")
            if y.lower() == "y":
                self.print_failures()
