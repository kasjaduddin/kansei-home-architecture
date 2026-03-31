#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;

public class AndroidManifestPostProcessor : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1;

    public void OnPostGenerateGradleAndroidProject(string basePath)
    {
        string manifestPath = Path.Combine(basePath, "src", "main", "AndroidManifest.xml");

        XmlDocument doc = new XmlDocument();
        doc.Load(manifestPath);

        XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("android", "http://schemas.android.com/apk/res/android");

        XmlElement manifest = doc.DocumentElement;
        string androidNs = "http://schemas.android.com/apk/res/android";

        // Add INTERNET permission if not present
        XmlNodeList permissions = manifest.SelectNodes(
            "//uses-permission[@android:name='android.permission.INTERNET']", nsMgr);

        if (permissions.Count == 0)
        {
            XmlElement permission = doc.CreateElement("uses-permission");
            permission.SetAttribute("name", androidNs, "android.permission.INTERNET");
            manifest.InsertBefore(permission, manifest.FirstChild);
            UnityEngine.Debug.Log("[AndroidManifestPostProcessor] Added INTERNET permission");
        }

        // Add usesCleartextTraffic to application element
        XmlNode application = manifest.SelectSingleNode("application");
        if (application != null)
        {
            XmlAttribute cleartextAttr = doc.CreateAttribute("android", "usesCleartextTraffic", androidNs);
            cleartextAttr.Value = "true";
            application.Attributes.SetNamedItem(cleartextAttr);
            UnityEngine.Debug.Log("[AndroidManifestPostProcessor] Set usesCleartextTraffic=true");
        }

        doc.Save(manifestPath);
        UnityEngine.Debug.Log("[AndroidManifestPostProcessor] Manifest patched successfully");
    }
}
#endif