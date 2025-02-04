using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour {
    public static AudioManager Instance;
    
    public AudioMixer mixer;
    
    // music
    public AudioSource music;
    public AudioSource level0;
    public AudioSource level1;
    public AudioSource level2;
    // SFX
    public AudioSource recordScratch;
    public AudioSource digThis;

    public AudioSource rolling;
    public AudioSource push;
    public AudioSource jump;
    public AudioSource railland;
    public AudioSource railgrind;
    public AudioSource trick;
    public AudioSource slide; // ramp
    public AudioSource safeLanding;
    public AudioSource skateboardFalling;
    public AudioSource skid;

    public AudioSource scratchAscending;
    public AudioSource scratchDescending;

    public AudioSource key;
    public AudioSource keyTwang;

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.parent = null;
        DontDestroyOnLoad(this);
    }

    private void Start() {
        SetVolume("MasterVolume", PlayerPrefs.GetFloat("MasterVolume", 0.4f));
        SetVolume("MusicVolume", PlayerPrefs.GetFloat("MusicVolume", 1f));
        SetVolume("SFXVolume", PlayerPrefs.GetFloat("SFXVolume", 1f));

        key.ignoreListenerPause = true;
        trick.ignoreListenerPause = true;
        scratchDescending.ignoreListenerPause = true;

        SceneManager.activeSceneChanged += (oldscene, newscene) => {
            AddCallbacks();
            rolling.Stop();
            
            string sceneName = newscene.name;
            music.Stop();
            level0.Stop();
            level1.Stop();
            level2.Stop();
            if(sceneName == "Level0")
            {
                level0.Play();
            }
            else if(sceneName == "Level1")
            {
                level1.Play();
            }
            else if(sceneName == "Level2")
            {
                level2.Play();
            }
            else
            {
                music.Play();
            }
        };
        AddCallbacks();
    }

    private void AddCallbacks() {
        if (Player.Instance != null) {
            Player.Instance.OnJump += () => {
                jump.Play();
            };
            Player.Instance.OnSafeLanding += score => {
                safeLanding.Play();
                digThis.Play();
            };
            Player.Instance.OnUnsafeLanding += () => {
                skateboardFalling.Play();
            };
            Player.Instance.OnWipeOut += () => {
                recordScratch.Play();
                skid.Play();
            };
            Player.Instance.OnStateChange += state => {
                if (state == Player.State.OnRail) {
                    railland.Play();
                    railgrind.Play();
                }
                else railgrind.Stop();

                if (state == Player.State.OnRamp) {
                    slide.Play();
                }
                else slide.Stop();
            };
        }

        if(TrickManager.Instance) TrickManager.Instance.OnCompleteTrick += trickWord => {
            if (trickWord.Equals("push")) {
                push.Play();
            }
        };
    }

    private void Update() {
        var player = Player.Instance;
        if (player != null) {
            // play rolling sound
            if (player.GetSpeed() > 0 && (player.state == Player.State.OnGround || player.state == Player.State.OnRamp)) {
                if(!rolling.isPlaying) rolling.Play();
            }
            else if(rolling.isPlaying) rolling.Stop();
            // adjust volume based on player speed
            rolling.volume = Mathf.Lerp(0.01f, 0.5f, (player.GetSpeed() - player.minSpeed) / 2*player.maxSpeed);
        }
    }

    public void PlaySafeSound() {
        scratchAscending.Play();
    }
    public void PlayUnsafeSound() {
        scratchDescending.Play();
    }

    public void PlayTypingSound() {
        key.Play();
    }
    public void PlayTypingWrongSound() {
        keyTwang.Play();
    }

    public void PlayTrickSound() {
        trick.Play();
    }

    public void SetVolume(string mixerParameter, float value) {
        mixer.SetFloat(mixerParameter, Mathf.Log10(value) * 20f);
        PlayerPrefs.SetFloat(mixerParameter, value);
    }

    public void PlaySelectSound() {
        PlayTypingSound();
    }

    public void PlaySubmitSound() {
        PlayTrickSound();
    }

    public void PlayBackSound() {
        PlayTrickSound();
        PlayUnsafeSound();
    }

    public void PauseAudio() {
        rolling.Stop();
    }
    public void ResumeAudio() {
        
    }
}
