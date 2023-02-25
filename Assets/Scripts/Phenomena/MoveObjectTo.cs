using UnityEngine;
using Cysharp.Threading.Tasks;

public static class MoveObject
{
    public static async UniTask Linear(GameObject objectToMove, Vector3 to, float moveTimeInSeconds)
    {
        await WithCurve(objectToMove, to, AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f), moveTimeInSeconds);
    }

    static async UniTask WithCurve(GameObject objectToMove, Vector3 moveTo, AnimationCurve moveCurve, float moveTimeInSeconds)
    {
        Vector3 originalPosition = objectToMove.transform.position;
        float progress = 0.0f;
        while (progress < 1.0)
        {
            await UniTask.DelayFrame(1);
            progress += Time.deltaTime / moveTimeInSeconds;
            objectToMove.transform.position = Vector3.Lerp(originalPosition, moveTo, moveCurve.Evaluate(progress));
        }
    }
}