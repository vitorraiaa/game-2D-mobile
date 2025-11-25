using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndGameDeathController : MonoBehaviour
{
    [Header("Tempo de exibição da cutscene")]
    [SerializeField] private float waitTime = 7f;

    [Header("Fade (painel com FadeManager)")]
    [SerializeField] private FadeManager fade; // arraste o FadePanel com o script FadeManager

    [Header("Cena fallback (caso não tenha lastLevelIndex)")]
    [SerializeField] private string fallbackSceneName = "canva_menu";

    private void Start()
    {
        StartCoroutine(RunDeathCutscene());
    }

    private IEnumerator RunDeathCutscene()
    {
        // Fade-in: preto -> cena de morte
        if (fade != null)
            yield return fade.FadeIn();

        // tempo para o jogador ler/assistir a cutscene
        yield return new WaitForSeconds(waitTime);

        // Fade-out: cena de morte -> preto
        if (fade != null)
            yield return fade.FadeOut();

        // Decide pra onde voltar
        if (DeathData.lastLevelIndex >= 0)
        {
            // Volta pro level em que o player morreu
            SceneManager.LoadScene(DeathData.lastLevelIndex);
        }
        else
        {
            // Se por algum motivo não tiver dado tempo de salvar, vai pra cena fallback (menu, por exemplo)
            if (!string.IsNullOrEmpty(fallbackSceneName))
            {
                SceneManager.LoadScene(fallbackSceneName);
            }
            else
            {
                Debug.LogWarning("[EndGameDeathController] Nenhum lastLevelIndex válido e fallbackSceneName vazio.");
            }
        }
    }
}
