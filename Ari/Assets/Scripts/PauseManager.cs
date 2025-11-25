using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Sistema de Pause completo para mobile
/// Gerencia o estado do jogo (pausado/jogando) e controla o menu de pause
/// </summary>
public class PauseManager : MonoBehaviour
{
    [Header("Referências UI")]
    public Button pauseButton;          // Botão com as duas hastes
    public GameObject pauseMenuPanel;   // Panel do menu de pause
    
    [Header("Botões do Menu")]
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;
    public Button quitButton;
    
    [Header("Configurações")]
    public bool pauseOnStart = false;
    public KeyCode pauseKeyPC = KeyCode.Escape; // Para testar no PC
    
    private bool isPaused = false;
    public static PauseManager Instance { get; private set; }

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Configura o botão de pause
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(TogglePause);
        }

        // Configura os botões do menu
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
        
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
        
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(LoadMainMenu);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // Garante que o jogo inicia despausado
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        Time.timeScale = pauseOnStart ? 0 : 1;
        isPaused = pauseOnStart;
    }

    void Update()
    {
        // Suporte para tecla ESC no PC (para testar)
        if (Input.GetKeyDown(pauseKeyPC))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Para o tempo do jogo
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
        
        // Esconde o botão de pause quando o menu está aberto
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(false);

        // Opcional: pausa o áudio
        AudioListener.pause = true;
        
        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Retorna o tempo normal
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        // Mostra o botão de pause novamente
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(true);

        // Resume o áudio
        AudioListener.pause = false;
        
        Debug.Log("Game Resumed");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // Importante resetar antes de recarregar
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        // Troque "MainMenu" pelo nome da sua cena de menu principal
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
        
        Debug.Log("Quit Game");
    }

    public bool IsPaused()
    {
        return isPaused;
    }

    void OnDestroy()
    {
        // Garante que o tempo volta ao normal se o objeto for destruído
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}