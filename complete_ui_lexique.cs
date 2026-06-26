using System;
using System.Diagnostics;
using System.Text;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI; // Button
using Newtonsoft.Json;
using TMPro; // Input and Dynamic Text
//v6.1.2

[Serializable]
public class gloss_json_parser
{
    public string gloss;
    public int printFrench;
	public string french;
}

public class complete_ui_lexique : MonoBehaviour
{
	
	//=========================== CUDA capabilities checking var and funct ================================================
    // --- CUDA DRIVER API PINVOKES ---
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("nvcuda.dll", EntryPoint = "cuInit")]
    private static extern int cuInit(uint flags);

    [DllImport("nvcuda.dll", EntryPoint = "cuDeviceGet")]
    private static extern int cuDeviceGet(out int device, int ordinal);

    [DllImport("nvcuda.dll", EntryPoint = "cuDeviceGetAttribute")]
    private static extern int cuDeviceGetAttribute(out int pi, int attrib, int dev);

    // CUDA Driver API Constants
    private const int CU_DEVICE_ATTRIBUTE_COMPUTE_CAPABILITY_MAJOR = 75;
    private const int CUDA_SUCCESS = 0;

    public bool IsGPUNewEnoughForWhisper()
    {
        // 1. Is it even an NVIDIA card driver environment?
        IntPtr cudaLib = LoadLibrary("nvcuda.dll");
        if (cudaLib == IntPtr.Zero)
        {
            UnityEngine.Debug.LogWarning("nvcuda.dll not found. System does not have an active NVIDIA driver layer.");
            return false;
        }
        FreeLibrary(cudaLib); // Unload the handle so we can initialize safely via the driver API

        try
        {
            // 2. Initialize the CUDA Driver API
            if (cuInit(0) != CUDA_SUCCESS) return false;

            // 3. Get handle for the primary GPU (Device 0)
            if (cuDeviceGet(out int device, 0) != CUDA_SUCCESS) return false;

            // 4. Extract the Major Compute Capability version
            if (cuDeviceGetAttribute(out int majorVersion, CU_DEVICE_ATTRIBUTE_COMPUTE_CAPABILITY_MAJOR, device) == CUDA_SUCCESS)
            {
                UnityEngine.Debug.Log($"Detected CUDA Compute Capability Major: {majorVersion}");
                
                // whisper.cpp needs Maxwell (5) or higher
                return majorVersion >= 5;
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Error reading CUDA hardware values: {e.Message}");
        }

        return false;
    }

	private bool isReady4Whisper; 
	//=========================== End of CUDA capabilities checking var and funct ========================================

	// Predefine default factory list containing alphabet letters
    private HashSet<string> _allowedList = new HashSet<string>(StringComparer.Ordinal)
    { 
		"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
		"N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
		"AIDER", "AIMER", "ALLER", "ATTENTE", "AU_REVOIR", "AUJOURD'HUI",
		"AVOIR_MAL", "BIEN", "BON", "BONJOUR", "CA_VA", "COEUR", "COULOIR",
		"COMMENT", "COMPRENDRE", "CONTENT", "COUCOU", "D'ACCORD", "ETUDIANT",
		"FAIRE", "FOND", "GAUCHE", "HABITER", "JEUX_VIDEO", "LOISIR", "MERCI",
		"MOI", "NOM", "NON", "NOUS", "OU", "OUI", "PARIS", "PAS", "PENSER",
		"PLAGE", "POSSIBLE", "QUOI", "RENCONTRER", "RENDEZ_VOUS", "SALLE",
		"TOI", "VACANCES", "VENIR", "VOUS"
    };

	// 1. Force UTF-8 WITHOUT the BOM (the 'false' parameter does this)
	Encoding utf8WithoutBom = new UTF8Encoding(false);

	//=============== Whisper =============================
	private Process process;
	private ConcurrentQueue<string> outputQueue = new ConcurrentQueue<string>();
	private ConcurrentQueue<string> errorQueue = new ConcurrentQueue<string>();
	private volatile int whisperPID;
	private volatile bool isWhisperReady = false;
	
	// Define this as a class member variable
	private gloss_json_parser stdout_1_json_parser = new gloss_json_parser();
	
	private Task _stdoutTask;
	private Task _stderrTask;
	//=====================================================
	
	private Process process2;
	private ConcurrentQueue<string> outputQueue2 = new ConcurrentQueue<string>();
	private ConcurrentQueue<string> errorQueue2 = new ConcurrentQueue<string>();
	
	private volatile bool isSpaCyReady = false;
	
    private Animator anim;
    
    // The queue now holds the exact name of ANY animation to fire
	public ConcurrentQueue<string> animationQueue = new ConcurrentQueue<string>();
    private volatile bool isPlayingAnimation = false;	

	// --- Thread Control Variables ---
    private bool _isRunning = true;

	// private float elapsedTime = 0f;
    // private float targetDuration = 2.6f; // 2.6 seconds (1.5 grace + 1.1 sample)
	
	private Task _stdoutTask2;
	private Task _stderrTask2;

	[Header("Last 7 translated sentences history")]
	// [Tooltip("Drag and drop TextMeshPro UI text here")]
	[SerializeField] private TextMeshProUGUI historyText;

	// A dedicated queue so the UI updates instantly, ignoring animation delays
	private ConcurrentQueue<string> uiQueue = new ConcurrentQueue<string>();
	private int sentenceCount = 0;
	
	[Header("User Input Box")]
    public TMP_InputField userInputField;
	public RectTransform rectTransformBottomSketchingAnchor;
	
	private StreamWriter process2Writer;
	
	[Header("Voice/Text mode toggle")]
    public Button toggleButton;
	
	[Header("Mode indicator")]
	[SerializeField] private TextMeshProUGUI modeText;
	
	private volatile bool isMuted = true;
	
	[Header("Whisper loading indicator")]
	[SerializeField] private TextMeshProUGUI loadingIndicator;
	
	[Header("Exit button")]
    public Button exitButton;
	
	[Header("Customization button")]
    public Button customButton;
	
	[Header("Confirm button")]
    public Button confirmButton;
	
	[Header("Main Canvas")]
    [SerializeField] private Canvas canvasOne;
    [SerializeField] private GraphicRaycaster raycasterOne;

    [Header("Customization Canvas")]
    [SerializeField] private Canvas canvasTwo;
    [SerializeField] private GraphicRaycaster raycasterTwo;

    void Start()
    {
		anim = GetComponent<Animator>();

		isReady4Whisper = IsGPUNewEnoughForWhisper();
		// isReady4Whisper = false;

		if (isReady4Whisper)
		{
		RunWhisperLink();
		_stdoutTask = Task.Run(stdoutProcess);
		_stderrTask = Task.Run(stderrProcess);
		}
	
        RunTextboxLink();
		_stdoutTask2 = Task.Run(stdoutProcess2);
		_stderrTask2 = Task.Run(stderrProcess2);
		

		// This tells Unity: "When the user presses Enter, run the OnInputSubmitted method."
		if (userInputField != null)
		{
			userInputField.onSubmit.AddListener(OnInputSubmitted);
			userInputField.interactable = false; //disabled until spaCy and Whisper or spaCy (if Whisper not supported) is ready
			loadingIndicator.text = "Chargement...";
			
			// Extend user input box if whisper is not supported
			if (!isReady4Whisper)
			{
				SetMarginsBottomSketchingAnchor(50, 50);
			}
		}
		else
		{
			UnityEngine.Debug.LogWarning("Input Field was not assigned in Unity Inspector");
		}

		if (toggleButton != null)
		{
			if (isReady4Whisper) 
			{
				toggleButton.onClick.AddListener(OnToggleClicked);
				toggleButton.interactable = false; //disabled until spaCy and Whisper or spaCy (if Whisper not supported) is ready
				modeText.text = isReady4Whisper ? "VOIX" : "TEXT";
			}
			else
			{
				toggleButton.gameObject.SetActive(false);
			}
		}
		else
		{
			UnityEngine.Debug.LogWarning("Toogle Button was not assigned in Unity Inspector");
		}

		if (exitButton != null)
		{
			exitButton.gameObject.SetActive(false);
			exitButton.onClick.AddListener(Application.Quit);
		}
		else
		{
			UnityEngine.Debug.LogWarning("Exit Button was not assigned in Unity Inspector");
		}
		
		if (customButton != null)
		{
			customButton.gameObject.SetActive(false);
			customButton.onClick.AddListener(ShowCustomizationCanvas);
		}
		else
		{
			UnityEngine.Debug.LogWarning("Customization Button was not assigned in Unity Inspector");
		}
		
		if (confirmButton != null)
		{			
			confirmButton.onClick.AddListener(ShowMainCanvas);
		}
		else
		{
			UnityEngine.Debug.LogWarning("Customization Button was not assigned in Unity Inspector");
		}
		
		SetCanvasState(canvasTwo, raycasterTwo, false);
    }

	void RunWhisperLink()
	{
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = $"{Application.streamingAssetsPath}\\python\\python.exe";
        start.Arguments = $"-u \"{Application.streamingAssetsPath}/python/AmySign_code/link.py\"";
        start.UseShellExecute = false;
        start.CreateNoWindow = true;
		start.RedirectStandardOutput = true;
		start.RedirectStandardError = true;

        // start.StandardOutputEncoding = Encoding.UTF8;

		start.StandardOutputEncoding = utf8WithoutBom;
		start.StandardErrorEncoding = utf8WithoutBom;

        process = new Process();
        process.StartInfo = start;
		
        process.OutputDataReceived += (sender, args) => {
            if (!string.IsNullOrEmpty(args.Data))
            {
				outputQueue.Enqueue(args.Data);
            }
        };

        process.ErrorDataReceived += (sender, args) => {
            if (!string.IsNullOrEmpty(args.Data))
            {
				errorQueue.Enqueue(args.Data);
            }
        };

        process.Start();
		UnityEngine.Debug.Log($"Whisper Link PID: {process.Id}\n");
        process.BeginOutputReadLine();
		process.BeginErrorReadLine();		
	}

	void RunTextboxLink()
    {
		ProcessStartInfo start2 = new ProcessStartInfo();
        start2.FileName = $"{Application.streamingAssetsPath}\\python\\python.exe";
        start2.Arguments = $"-u \"{Application.streamingAssetsPath}/python/AmySign_code/link_textbox.py\"";
        start2.UseShellExecute = false;
        start2.CreateNoWindow = true;
		start2.RedirectStandardOutput = true;
		start2.RedirectStandardError = true;
		start2.RedirectStandardInput = true;

        // start2.StandardOutputEncoding = Encoding.UTF8;
		// start2.StandardInputEncoding = Encoding.UTF8;
        // start2.StandardErrorEncoding = Encoding.UTF8;

		start2.StandardOutputEncoding = utf8WithoutBom;
		start2.StandardInputEncoding = utf8WithoutBom;
		start2.StandardErrorEncoding = utf8WithoutBom;
		
        process2 = new Process();
        process2.StartInfo = start2;
		
        process2.OutputDataReceived += (sender, args) => {
            if (!string.IsNullOrEmpty(args.Data))
            {
				outputQueue2.Enqueue(args.Data);
            }
        };

        process2.ErrorDataReceived += (sender, args) => {
            if (!string.IsNullOrEmpty(args.Data))
            {
				errorQueue2.Enqueue(args.Data);
            }
        };

        process2.Start();
		UnityEngine.Debug.Log($"Textbox Link PID: {process2.Id}\n");
        process2.BeginOutputReadLine();
		process2.BeginErrorReadLine();
		process2Writer = process2.StandardInput;
    }

	private void stdoutProcess()
	{
		while (_isRunning)
		{
			while (outputQueue.TryDequeue(out string jsonString))
			{
				if (!isMuted)
				{
					// UnityEngine.Debug.Log($"Parsing JSON string {jsonString}");
					try 
					{
						// Each 'jsonString' here is guaranteed to be one clean line
						JsonUtility.FromJsonOverwrite(jsonString, stdout_1_json_parser);
						
						// UnityEngine.Debug.Log($"Gloss: {stdout_1_json_parser.gloss}, printFrench: {stdout_1_json_parser.printFrench}, French: {stdout_1_json_parser.french}");
						
						// if (IsAllowedStatic(stdout_1_json_parser.gloss)){
							// UnityEngine.Debug.Log($"{stdout_1_json_parser.gloss} is recognized");
						// }
						
						string textToDisplay = (stdout_1_json_parser.printFrench == 1) ? stdout_1_json_parser.french : null;
						uiQueue.Enqueue(textToDisplay);
						
						if (_allowedList.Contains(stdout_1_json_parser.gloss)){
							// UnityEngine.Debug.Log($"{stdout_1_json_parser.gloss} is recognized");
							animationQueue.Enqueue(stdout_1_json_parser.gloss);
						}
						// else 
						// {
							// UnityEngine.Debug.LogWarning($"Unknown {stdout_1_json_parser.gloss}");
						// }

					}
					catch (Exception e)
					{
						UnityEngine.Debug.LogError($"Parse Error: {e.Message}");
					}
				}
			}
		}
    }
	
	private void stderrProcess()
	{
		while (_isRunning)
		{	
			while (errorQueue.TryDequeue(out string errorMsg))
			{
				ReadOnlySpan<char> spanMsg = errorMsg.AsSpan();
				
				if (spanMsg.StartsWith("pid")){
					int.TryParse(spanMsg.Slice(3), out int num); //whisper send pidxxxxx to stderr with xxxxx its pid
					whisperPID = num; // if whisperPID is used directly in TryParse, it will not be treated as volatile
				} 
				else if (spanMsg.SequenceEqual("READY"))
				{
					isWhisperReady = true;
				}
				UnityEngine.Debug.Log($"stderr 1: {errorMsg}");
			}
		}
	}
	
	private void stdoutProcess2()
	{
		while (_isRunning)
		{
			while (outputQueue2.TryDequeue(out string convertedGlosses))
			{
				// UnityEngine.Debug.LogWarning(convertedGlosses);
				if (isMuted && _allowedList.Contains(convertedGlosses))
				{
					animationQueue.Enqueue(convertedGlosses);
				}
			}
		}
    }
	
	private void stderrProcess2()
	{
		while (_isRunning)
		{	
			while (errorQueue2.TryDequeue(out string errorMsg))
			{
				ReadOnlySpan<char> spanMsg = errorMsg.AsSpan();
				if (spanMsg.SequenceEqual("READY"))
				{
					isSpaCyReady = true;
				}
				UnityEngine.Debug.Log($"stderr 2: {errorMsg}");
			}
		}
	}	
	
	void Update()
    {	
        // 1. Better ConcurrentQueue practice: TryDequeue directly instead of checking .Count
        // 2. Only try to dequeue if we are NOT currently playing an animation
        if (!isPlayingAnimation)
        {
			if (animationQueue.TryDequeue(out string nextStateName))
			{
				// Start the coroutine to handle playback and timing
				StartCoroutine(PlayNextRoutine(nextStateName));
			} 
			else // Whisper VAD grace time is 1500 ms, COUCOU only start after the grace period + sample time (1100)
			{
				//elapsedTime += Time.deltaTime;
				StartCoroutine(PlayNextRoutine("Idle"));
				// if (elapsedTime > targetDuration) {
					// StartCoroutine(PlayNextRoutine("COUCOU"));
				// }
			}
        }
		
		if (uiQueue.TryDequeue(out string uiText))
		{
			if (historyText != null && uiText != null)
			{
				if (sentenceCount < 7) // Display of 7 sentences (1 -> 7 = 7 sentences)
				{
					historyText.text += uiText;
					sentenceCount++;
				}
				else 
				{
					historyText.text = uiText;
					sentenceCount = 1;
				}
			}
		}
		
		
		// Change of objects has to be done in main thread
		if (isReady4Whisper && isWhisperReady)
		{
			isWhisperReady = false; // Reset the flag as it only need to run once
			initUI();
		}
		
		if (!isReady4Whisper && isSpaCyReady)
		{
			isSpaCyReady = false; // Reset the flag as it only need to run once
			initUI();
		}

    }

	private void initUI()
	{
		modeText.text = "TEXT";
		loadingIndicator.text = "Que souhaitez vous dire ?";
		userInputField.interactable = true;
		toggleButton.interactable = isReady4Whisper;
		exitButton.gameObject.SetActive(true);
		customButton.gameObject.SetActive(true);
		confirmButton.gameObject.SetActive(true);
	}

    // Change void to IEnumerator to make this a Coroutine
    private System.Collections.IEnumerator PlayNextRoutine(string nextStateName)
    {
		// elapsedTime = 0f;
        // UnityEngine.Debug.Log($"Playing: {nextStateName}");
        
        // Lock the queue
        isPlayingAnimation = true;

        // Start the blend
        anim.CrossFade(nextStateName, 0.044f); // Animlation Quirk free thredshold <=0.446, visually no difference between 0.044 and 0.446

        // CRITICAL: Wait 1 frame to let the Animator update its internal state machine
        yield return null; 

        // Get the length of the animation we just transitioned into (Layer 0)
        float animDuration = anim.GetCurrentAnimatorStateInfo(0).length;

        // Pause this specific method until the animation finishes
        // (We subtract 0.3f so the next animation starts blending BEFORE this one completely ends, 
        // ensuring a smooth continuous chain of movements)
        yield return new WaitForSeconds(animDuration - 0.044f);

        // Unlock the queue! The next Update() frame will grab the next animation.
        isPlayingAnimation = false;
    }
	
	// This method only fires when the user hits 'Enter' in the text box
    private void OnInputSubmitted(string inputText)
    {
        if (string.IsNullOrWhiteSpace(inputText)) return;

        UnityEngine.Debug.Log("User submitted: " + inputText);

		SendDataToPython(inputText);

		inputText += '\n';
        uiQueue.Enqueue(inputText);

        // Optional: Clear the input field after they press enter
        userInputField.text = "";
        
        // Optional: Keep the text box focused so they can keep typing immediately
        userInputField.ActivateInputField(); 
    }

	public void SendDataToPython(string message)
    {
        if (process2Writer != null && process2 != null && !process2.HasExited)
        {
            // process2Writer.WriteLine(message);
            // process2Writer.Flush(); // Ensure the data is pushed immediately
            UnityEngine.Debug.Log("[Unity] Sent to Python: " + message);
			// process2Writer.WriteLine("é");
			process2Writer.WriteLine(message);
			process2Writer.Flush();
        }
        else
        {
            UnityEngine.Debug.LogWarning("[Unity] Cannot send data. Python process is not running.");
        }
    }

	private void OnToggleClicked()
    {
        // Toggle the boolean instantly
        isMuted = !isMuted;
        UnityEngine.Debug.Log($"Mute state is now: {isMuted}");
		
		if (userInputField == null) return;
		modeText.text = isMuted ? "TEXT" : "VOIX";
		userInputField.interactable = isMuted;
    }

	public void ShowMainCanvas()
    {
        // Enable Canvas 1, Disable Canvas 2
        SetCanvasState(canvasOne, raycasterOne, true);
        SetCanvasState(canvasTwo, raycasterTwo, false);
    }

    public void ShowCustomizationCanvas()
    {
        // Disable Canvas 1, Enable Canvas 2
        SetCanvasState(canvasOne, raycasterOne, false);
        SetCanvasState(canvasTwo, raycasterTwo, true);
		isMuted = true;
		if (userInputField == null) return;
		modeText.text = "TEXT";
		userInputField.interactable = true;
		
    }

    // A clean helper method to toggle both the canvas and its click-detection
    private void SetCanvasState(Canvas canvas, GraphicRaycaster raycaster, bool state)
    {
        if (canvas != null) canvas.enabled = state;
        if (raycaster != null) raycaster.enabled = state;
    }

	void KillProcessById(int pid)
	{
		try
		{
			// The 'using' block automatically calls Dispose() 
			// when the code exits the scope.
			using (Process proc = Process.GetProcessById(pid))
			{
				proc.Kill();
				proc.WaitForExit();
				UnityEngine.Debug.Log($"Successfully killed grand child process: {pid}");
			} 
		}
		catch (System.ArgumentException)
		{
			UnityEngine.Debug.LogWarning("Process already closed.");
		}
		catch (System.Exception e)
		{
			UnityEngine.Debug.LogError($"Error: {e.Message}");
		}
	}

    public void SetMarginsBottomSketchingAnchor(float left, float right)
    {
        if (rectTransformBottomSketchingAnchor == null) return;

        // offsetMin.x controls the LEFT side
        // We keep the current bottom value (offsetMin.y) intact
        rectTransformBottomSketchingAnchor.offsetMin = new Vector2(left, rectTransformBottomSketchingAnchor.offsetMin.y);

        // offsetMax.x controls the RIGHT side
        // Note: It needs to be NEGATIVE because it moves inward from the right edge
        rectTransformBottomSketchingAnchor.offsetMax = new Vector2(-right, rectTransformBottomSketchingAnchor.offsetMax.y);
    }

	void OnApplicationQuit()
    {
		_isRunning = false;
		
		if (isReady4Whisper)
		{
			KillProcessById(whisperPID);
			
			// Child process is programmed to exit when its own child exits
			// The following line checks that expected behaviour
			process.WaitForExit();
			
			UnityEngine.Debug.Log($"Successfully killed child process: {process.Id}");
		}
		

		if (process2Writer != null)
        {
            // Send the quit command; an UUID so user input can't accidentally kill glosses_text.py
            process2Writer.WriteLine("fffa09deb551437381d7567274bae72e");
            process2Writer.Close();
        }
		
		// It's good practice to remove listeners when the object is destroyed
        if (userInputField != null)
        {
            userInputField.onSubmit.RemoveListener(OnInputSubmitted);
        }
		
		if (toggleButton != null && isReady4Whisper) // Listener is not assigned if whisper is not supported
        {
            toggleButton.onClick.RemoveListener(OnToggleClicked);
        }

    }
}