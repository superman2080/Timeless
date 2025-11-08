using UnityEngine;

public static class Utils
{
    public static Vector3 GetTopViewportPosition(float offset)
    {
        Vector3 viewportPoint = new Vector3(0.5f, 1f + offset, 0f);

        // 뷰포트 좌표를 ray로 변환
        Ray ray = Camera.main.ViewportPointToRay(viewportPoint);

        // y=0 평면과의 교차점 계산
        if (RayPlaneIntersection(ray, Vector3.zero, Vector3.up, out Vector3 hitPoint))
        {
            return hitPoint;
        }

        Debug.LogWarning("Ray does not intersect with y=0 plane");
        return Vector3.zero;
    }

    public static Vector3 GetBottomViewportPosition(float offset)
    {
        Vector3 viewportPoint = new Vector3(0.5f, -offset, 0f);

        // 뷰포트 좌표를 ray로 변환
        Ray ray = Camera.main.ViewportPointToRay(viewportPoint);

        // y=0 평면과의 교차점 계산
        if (RayPlaneIntersection(ray, Vector3.zero, Vector3.up, out Vector3 hitPoint))
        {
            return hitPoint;
        }

        Debug.LogWarning("Ray does not intersect with y=0 plane");
        return Vector3.zero;
    }

    public static Vector3 GetRightViewportPosition(float offset)
    {
        Vector3 viewportPoint = new Vector3(1f + offset, 0.5f, 0f);

        // 뷰포트 좌표를 ray로 변환
        Ray ray = Camera.main.ViewportPointToRay(viewportPoint);

        // y=0 평면과의 교차점 계산
        if (RayPlaneIntersection(ray, Vector3.zero, Vector3.up, out Vector3 hitPoint))
        {
            return hitPoint;
        }

        Debug.LogWarning("Ray does not intersect with y=0 plane");
        return Vector3.zero;
    }

    public static Vector3 GetLeftViewportPosition(float offset)
    {
        Vector3 viewportPoint = new Vector3(-offset, 0.5f, 0f);

        // 뷰포트 좌표를 ray로 변환
        Ray ray = Camera.main.ViewportPointToRay(viewportPoint);

        // y=0 평면과의 교차점 계산
        if (RayPlaneIntersection(ray, Vector3.zero, Vector3.up, out Vector3 hitPoint))
        {
            return hitPoint;
        }

        Debug.LogWarning("Ray does not intersect with y=0 plane");
        return Vector3.zero;
    }

    private static bool RayPlaneIntersection(Ray ray, Vector3 planePoint, Vector3 planeNormal, out Vector3 intersection)
    {
        intersection = Vector3.zero;

        float denominator = Vector3.Dot(ray.direction, planeNormal);

        // Ray가 평면과 평행한 경우
        if (Mathf.Abs(denominator) < 0.0001f)
            return false;

        float t = Vector3.Dot(planePoint - ray.origin, planeNormal) / denominator;

        // Ray가 평면과 반대 방향을 향하는 경우
        if (t < 0)
            return false;

        intersection = ray.origin + ray.direction * t;
        return true;
    }

}
