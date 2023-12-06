using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Azure;
using Azure.AI.OpenAI;
using static System.Environment;

class Program
{
    // This example requires environment variables named "OPEN_AI_KEY" and "OPEN_AI_ENDPOINT"
    // Your endpoint should look like the following https://YOUR_OPEN_AI_RESOURCE_NAME.openai.azure.com/
    static string openAIKey = "👉_____Azure_OpenAI_Key____"; 
    static string openAIEndpoint = "https://👉_____EndPoint____.openai.azure.com/";

    // Enter the deployment name you chose when you deployed the model.
    static string engine = "👉___Deployment__gpt35_____";

    // This example requires environment variables named "SPEECH_KEY" and "SPEECH_REGION"
    static string speechKey = "👉_____Azure_AI_Speech_Key____";
    static string speechRegion = "👉___eastasia___region___";

    // Prompts Azure OpenAI with a request and synthesizes the response.
    async static Task AskOpenAI(string prompt)
    {
        // Ask Azure OpenAI
        OpenAIClient client = new(new Uri(openAIEndpoint), new AzureKeyCredential(openAIKey));
        var ChatCompletionsOptions = new ChatCompletionsOptions()
        {
            Messages = {
                    new ChatMessage(ChatRole.System, "你是一個AI個人語音助理，會以口語化的方式，簡單扼要的回應用戶傳來的各種訊息與問題。"),
                    new ChatMessage(ChatRole.User, prompt),
                },
            MaxTokens = 300,
        };
        Response<ChatCompletions> completionsResponse = client.GetChatCompletions(engine, ChatCompletionsOptions);
        string text = completionsResponse.Value.Choices[0].Message.Content.Trim();
        Console.WriteLine($"Azure OpenAI 回覆: \n {text}");

        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        // The language of the voice that speaks.
        speechConfig.SpeechSynthesisVoiceName = "zh-TW-YunJheNeural";
        var audioOutputConfig = AudioConfig.FromDefaultSpeakerOutput();

        using (var speechSynthesizer = new SpeechSynthesizer(speechConfig, audioOutputConfig))
        {
            var speechSynthesisResult = await speechSynthesizer.SpeakTextAsync(text).ConfigureAwait(true);

            if (speechSynthesisResult.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine($"Speech synthesized to speaker for text: [{text}]");
            }
            else if (speechSynthesisResult.Reason == ResultReason.Canceled)
            {
                var cancellationDetails = SpeechSynthesisCancellationDetails.FromResult(speechSynthesisResult);
                Console.WriteLine($"Speech synthesis canceled: {cancellationDetails.Reason}");

                if (cancellationDetails.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"Error details: {cancellationDetails.ErrorDetails}");
                }
            }
        }
    }

    // Continuously listens for speech input to recognize and send as text to Azure OpenAI
    async static Task ChatWithOpenAI()
    {
        // Should be the locale for the speaker's language.
        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
        speechConfig.SpeechRecognitionLanguage = "zh-TW";

        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);
        var conversationEnded = false;

        while (!conversationEnded)
        {
            Console.WriteLine("Azure OpenAI 待命中，可說出 '停止' 或按下 Ctrl+C 結束對談.");

            // Get audio from the microphone and then send it to the TTS service.
            var speechRecognitionResult = await speechRecognizer.RecognizeOnceAsync();

            switch (speechRecognitionResult.Reason)
            {
                case ResultReason.RecognizedSpeech:
                    if (speechRecognitionResult.Text == "停止")
                    {
                        Console.WriteLine("結束對談.");
                        conversationEnded = true;
                    }
                    else
                    {
                        Console.WriteLine($"Recognized speech: {speechRecognitionResult.Text}");
                        await AskOpenAI(speechRecognitionResult.Text).ConfigureAwait(true);
                    }
                    break;
                case ResultReason.NoMatch:
                    Console.WriteLine($"No speech could be recognized: ");
                    break;
                case ResultReason.Canceled:
                    var cancellationDetails = CancellationDetails.FromResult(speechRecognitionResult);
                    Console.WriteLine($"Speech Recognition canceled: {cancellationDetails.Reason}");
                    if (cancellationDetails.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"Error details={cancellationDetails.ErrorDetails}");
                    }
                    break;
            }
        }
    }

    async static Task Main(string[] args)
    {
        try
        {
            await ChatWithOpenAI().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
