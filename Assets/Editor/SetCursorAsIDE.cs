using UnityEditor;
using UnityEngine;
using System.IO;

public class SetCursorAsIDE
{
    [MenuItem("Tools/Set Cursor as Script Editor")]
    public static void SetCursorEditor()
    {
        // Cursor 실행 파일 경로를 입력하세요.
        string cursorPath = @"C:\Users\user\AppData\Local\Programs\Cursor\Cursor.exe";
        if (!File.Exists(cursorPath))
        {
            EditorUtility.DisplayDialog("Error", "Cursor 실행 파일을 찾을 수 없습니다:\n" + cursorPath, "확인");
            return;
        }

        // Unity 외부 스크립트 에디터로 Cursor 지정
        EditorPrefs.SetString("kScriptsDefaultApp", cursorPath);
        EditorUtility.DisplayDialog("완료", "Cursor가 외부 스크립트 에디터로 설정되었습니다.", "확인");
    }
}