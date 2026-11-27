(function () {
  try {
    var latestVersion = null;

    function syncBadge(versionText) {
      if (!versionText) return;
      var badges = document.querySelectorAll(".md-source__fact--version");
      badges.forEach(function (el) {
        if (el.textContent !== versionText) {
          el.textContent = versionText;
        }
      });
    }

    function updateSessionStorage(versionText) {
      if (!versionText) return;
      for (var i = 0; i < sessionStorage.length; i++) {
        var k = sessionStorage.key(i);
        if (k && k.indexOf("__source") !== -1) {
          try {
            var v = JSON.parse(sessionStorage.getItem(k)) || {};
            v.version = versionText;
            sessionStorage.setItem(k, JSON.stringify(v));
          } catch (e) {
            sessionStorage.removeItem(k);
          }
        }
      }
    }

    // 1. Récupérer dynamiquement la dernière release officielle GitHub (sans jamais forcer une vieille version en dur)
    fetch("https://api.github.com/repos/Audiothor/MusicScoreManager/releases/latest", { cache: "no-store" })
      .then(function (r) { return r.ok ? r.json() : null; })
      .then(function (data) {
        if (data && data.tag_name) {
          latestVersion = data.tag_name;
          updateSessionStorage(latestVersion);
          syncBadge(latestVersion);
          // Re-synchroniser après le rendu asynchrone du composant md-source de MkDocs Material
          setTimeout(function () { syncBadge(latestVersion); }, 300);
          setTimeout(function () { syncBadge(latestVersion); }, 1000);
        }
      })
      .catch(function () {});

    document.addEventListener("DOMContentLoaded", function () {
      if (latestVersion) {
        syncBadge(latestVersion);
      }
    });
  } catch (err) {}
})();
