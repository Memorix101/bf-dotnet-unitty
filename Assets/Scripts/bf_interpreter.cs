using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class bf_interpreter : MonoBehaviour
{
    public TMP_InputField tmp_inputfield;
    public TMP_InputField tmp_console;
    public TMP_InputField tmp_register;
    public Image cursor;
    public Button start_btn;
    public Button stop_btn;
    public Button step_btn;
    private bool isRunning = false;
    private bool isStepping = false;
    private float speed = 10f;
    public Slider slider;
    public Text slider_speed;

    //bf-dotnet
    static byte[] register;
    static int[] loop_ptr;
    static int reg_id;
    static int loop_id;
    static bool skip_forward;
    static int skip_id;
    string code;

    void PrintConsole(string text)
    {
        tmp_console.text += text;
    }

    void PrintConsoleLine(string text)
    {
        tmp_console.text += $"{text}\n";
    }
    void PrintRegConsole(string text)
    {
        tmp_register.text += text;
    }

    void PrintregConsoleLine(string text)
    {
        tmp_register.text += $"{text}\n";
    }

    void add()
    {
        register[reg_id]++;
    }

    void sub()
    {
        register[reg_id]--;
    }

    void inc_cell()
    {
        if (reg_id + 1 > register.Length - 1)
        {
            Array.Resize<byte>(ref register, register.Length + 1);
        }
        reg_id++;
    }

    void dec_cell()
    {
        reg_id--;
    }

    void print()
    {
        if (register[reg_id] == 0)
        {
            PrintConsole($"{(char)32}");
        }
        else
        {
            PrintConsole($"{(char)register[reg_id]}");
        }

        //debug_print_cells();
    }

    void debug_print_cells()
    {
        tmp_register.text = string.Empty;
        //PrintConsole("\n");
        foreach (int r in register)
        {
            PrintRegConsole($"[{r}]");
        }

        PrintRegConsole("\n");
        string spacer = "";
        for (int i = 0; i < register.Length; i++)
        {
            if (reg_id == i)
            {
                for (int b = 0; b < reg_id; b++)
                {
                    spacer += $"[{register[b]}]";
                }

                for (int a = 0; a < spacer.ToString().Length; a++)
                {
                    PrintRegConsole($"-");
                }

                PrintRegConsole($"-^^");
                PrintRegConsole($"({reg_id + 1})");
            }
        }
        //PrintConsoleLine("\n" + spacer);
    }


    // Start is called before the first frame update
    void Start()
    {
        register = new byte[8];
        loop_ptr = new int[8];
        reg_id = 0;
        loop_id = -1;
        skip_forward = false;
        skip_id = 0;
        slider.value = (int)speed;
        slider_speed.text = $"{speed}";
    }


    private float currentChar = -1;
    private int last = -1;
    // Update is called once per frame
    void Update()
    {
        if (isRunning)
        {
            slider.interactable = false;
            /*start_btn.interactable = false;
            stop_btn.interactable = true;*/
            tmp_inputfield.interactable = false;
            if (!isStepping)
            {
                currentChar += speed * Time.deltaTime;
            }
            else
            {
                //step_btn.interactable = true;
            }

            if (currentChar > code.Length)
            {
                isRunning = false;
                currentChar = -1;
                last = -1;
            }
            else
            {
                Debug.Log($"{register[0]} - {register[1]}");
                if (last != (int) currentChar)
                {
                    Interpreter((int) currentChar);
                }

                last = (int) currentChar;

                //debug_print_cells();
                tmp_inputfield.textComponent.ForceMeshUpdate();
                // Get the index of the material used by the current character.
                int materialIndex = tmp_inputfield.textComponent.textInfo.characterInfo[(int) currentChar].materialReferenceIndex;
                // Get the vertex colors of the mesh used by this text element (character or sprite).
                Color32[] newVertexColors = tmp_inputfield.textComponent.textInfo.meshInfo[materialIndex].colors32;
                // Get the index of the first vertex used by this text element.
                int vertexIndex = tmp_inputfield.textComponent.textInfo.characterInfo[(int) currentChar].vertexIndex;
                if (tmp_inputfield.textComponent.textInfo.characterInfo[(int) currentChar].isVisible)
                {
                    Color32 c0 = new Color32((byte) 0, (byte) 255, (byte) 0, 255);
                    newVertexColors[vertexIndex + 0] = c0;
                    newVertexColors[vertexIndex + 1] = c0;
                    newVertexColors[vertexIndex + 2] = c0;
                    newVertexColors[vertexIndex + 3] = c0;
                    tmp_inputfield.textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                }
            }
        }
        else
        {
            code = tmp_inputfield.text;
            speed = (int)slider.value;
            slider_speed.text = $"{speed}";
            slider.interactable = true;
            tmp_inputfield.interactable = true;
            /*start_btn.interactable = true;
            step_btn.interactable = true;
            stop_btn.interactable = false;*/
        }
    }

    void Interpreter(int i)
    {
        //for (int i = 0; i < code.Length; i++)
        //{
        if (!skip_forward)
        {
            if (code[i] == '+')
            {
                add();
            }
            else if (code[i] == '-')
            {
                sub();
            }
            else if (code[i] == '>')
            {
                inc_cell();
            }
            else if (code[i] == '<')
            {
                dec_cell();
            }
            else if (code[i] == '.')
            {
                print();
            }
            else if (code[i] == '[')
            {
                if (register[reg_id] == 0)
                {
                    skip_forward = true;
                    skip_id++;
                }
                else
                {
                    if (loop_id + 1 > loop_ptr.Length - 1)
                    {
                        Array.Resize<int>(ref loop_ptr, loop_ptr.Length + 1);
                    }

                    loop_id++;
                    loop_ptr[loop_id] = i;
                }
            }
            else if (code[i] == ']' && register[reg_id] != 0)
            {
                i = loop_ptr[loop_id];
                currentChar = loop_ptr[loop_id];
            }
            else if (code[i] == ']' && register[reg_id] == 0)
            {
                loop_id--;
            }
        }
        else if (code[i] == '[' && skip_forward)
        {
            skip_id++;
        }
        else if (code[i] == ']' && skip_forward)
        {
            if (skip_id == 1)
            {
                skip_forward = false;
                skip_id = 0;
            }
            else
            {
                skip_id--;
            }
        }
        //}
        debug_print_cells();
    }

    public void RunCode()
    {
        //reset
        register = new byte[8];
        loop_ptr = new int[8];
        reg_id = 0;
        loop_id = -1;
        skip_forward = false;
        skip_id = 0;

        tmp_console.text = string.Empty;
        isRunning = true;
        isStepping = false;
        currentChar = -1;
        //PrintConsoleLine("bf-dotnet-core v0.9 - Copyright (c) 2021 Memorix101\n");
        //Interpreter(0);
    }

    public void StepBtn()
    {
        //tmp_console.text = string.Empty;
        isRunning = true;
        isStepping = true;
        currentChar++;
    }

    public void StopBtn()
    {
        isRunning = false;
    }
}