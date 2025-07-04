using LMKit.Graphics;
using LMKit.Model;
using LMKit.TextGeneration;
using LMKit.Translation;
using System.Text;


namespace translator
{
    internal class Program
    {
        //static readonly string DEFAULT_LLAMA3_1_8B_MODEL_PATH = @"https://huggingface.co/lm-kit/llama-3.1-8b-instruct-gguf/resolve/main/Llama-3.1-8B-Instruct-Q4_K_M.gguf?download=true";
        //static readonly string DEFAULT_GEMMA3_4B_MODEL_PATH = @"https://huggingface.co/lm-kit/gemma-3-4b-instruct-lmk/resolve/main/gemma-3-4b-it-Q4_K_M.lmk?download=true";
        //static readonly string DEFAULT_PHI4_MINI_3_8B_MODEL_PATH = @"https://huggingface.co/lm-kit/phi-4-mini-3.8b-instruct-gguf/resolve/main/Phi-4-mini-Instruct-Q4_K_M.gguf?download=true";
        static readonly string DEFAULT_PHI4_MINI_3_8B_MODEL_PATH = @"https://huggingface.co/lm-kit/phi-4-mini-3.8b-instruct-gguf/resolve/main/Phi-4-mini-Instruct-Q3_K_S.gguf?download=true";
        //static readonly string DEFAULT_QWEN3_8B_MODEL_PATH = @"https://huggingface.co/lm-kit/qwen-3-8b-instruct-gguf/resolve/main/Qwen3-8B-Q4_K_M.gguf?download=true";
        //static readonly string DEFAULT_MISTRAL_NEMO_12_2B_MODEL_PATH = @"https://huggingface.co/lm-kit/mistral-nemo-2407-12.2b-instruct-gguf/resolve/main/Mistral-Nemo-2407-12.2B-Instruct-Q4_K_M.gguf?download=true";
        //static readonly string DEFAULT_PHI4_14_7B_MODEL_PATH = @"https://huggingface.co/lm-kit/phi-4-14.7b-instruct-gguf/resolve/main/Phi-4-14.7B-Instruct-Q4_K_M.gguf?download=true";
        //static readonly string DEFAULT_GRANITE_3_3_8B_MODEL_PATH = @"https://huggingface.co/lm-kit/granite-3.3-8b-instruct-gguf/resolve/main/granite-3.3-8B-Instruct-Q4_K_M.gguf?download=true";
        static bool _isDownloading;

        private static bool ModelDownloadingProgress(string path, long? contentLength, long bytesRead)
        {
            _isDownloading = true;
            if (contentLength.HasValue)
            {
                double progressPercentage = Math.Round((double)bytesRead / contentLength.Value * 100, 2);
                Console.Write($"\rDownloading model {progressPercentage:0.00}%");
            }
            else
            {
                Console.Write($"\rDownloading model {bytesRead} bytes");
            }

            return true;
        }

        private static bool ModelLoadingProgress(float progress)
        {
            if (_isDownloading)
            {
                Console.Clear();
                _isDownloading = false;
            }

            Console.Write($"\rLoading model {Math.Round(progress * 100)}%");

            return true;
        }

        private static void Translation_AfterTextCompletion(object sender, LMKit.TextGeneration.Events.AfterTextCompletionEventArgs e)
        {
            Console.Write(e.Text);
        }

        static void Main(string[] args)
        {
            // Set an optional license key here if available. 
          
            LMKit.Licensing.LicenseManager.SetLicenseKey("019A500-180615-C694FC-832E61-A2D000-00A858-468995-310904-704070-EA");
            LMKit.Global.Runtime.EnableCuda = false;
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            Language destLanguage = Language.English; //set destination language supported by your model here.

            //Uri modelUri = new(DEFAULT_PHI4_MINI_3_1B_MODEL_PATH);
            Uri modelUri = new(@"C:\ModelGGUF\Phi-4-mini-Instruct-Q3_K_S.gguf");
            LM model = new(modelUri, 
                //storagePath: @"C:\ModelGGUF\",
                                    deviceConfiguration: new LM.DeviceConfiguration() { GpuLayerCount = 0 },
                                    downloadingProgress: ModelDownloadingProgress,
                                    loadingProgress: ModelLoadingProgress);

            Console.Clear();
            TextTranslation translator = new(model);

            translator.AfterTextCompletion += Translation_AfterTextCompletion;
            int translationCount = 0;

            while (true)
            {
                //Console.ForegroundColor = ConsoleColor.Green;

                //if (translationCount > 0)
                //{
                //    Console.Write("\n\n");
                //}

                //Console.Write($"Enter a text to translate in {destLanguage}:\n\n");
                //Console.ResetColor();

                //string text = Console.ReadLine();
                string text = "text";

                //if (string.IsNullOrWhiteSpace(text))
                //{
                //    break;
                //}

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"\nDetecting language...");
                Language inputLanguage = translator.DetectLanguage(text);
               // Console.Write($"\nTranslating from {inputLanguage}...\n");
               // Console.ResetColor();
                //_ = translator.Translate(text, destLanguage, new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token);
                translationCount++;
            }

            //Console.WriteLine("The program ended. Press any key to exit the application.");
          //  _ = Console.ReadKey();
        }
    }
}