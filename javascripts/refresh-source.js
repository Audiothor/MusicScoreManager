(function () {
  try {
    // 1. Invalider immédiatement tout cache sessionStorage obsolète antérieur à v3.2.14
    var expectedMinVersion = "v3.2.14";
    for (var i = sessionStorage.length - 1; i >= 0; i--) {
      var key = sessionStorage.key(i);
      if (key && key.indexOf("__source") !== -1) {
        try {
          var val = JSON.parse(sessionStorage.getItem(key));
          if (val && val.version && val.version !== expectedMinVersion) {
            val.version = expectedMinVersion;
            sessionStorage.setItem(key, JSON.stringify(val));
          }
        } catch (e) {
          sessionStorage.removeItem(key);
        }
      }
    }

    // 2. Mettre à jour le badge DOM immédiatement s'il a déjà été rendu avec l'ancienne valeur en cache
    function syncBadge(versionText) {
      var badges = document.querySelectorAll(".md-source__fact--version");
      badges.forEach(function (el) {
        if (el.textContent !== versionText) {
          el.textContent = versionText;
        }
      });
    }

    syncBadge(expectedMinVersion);
    document.addEventListener("DOMContentLoaded", function () {
      syncBadge(expectedMinVersion);
      setTimeout(function () { syncBadge(expectedMinVersion); }, 300);
    });

    // 3. Synchroniser avec la dernière release GitHub en direct
    fetch("https://api.github.com/repos/Audiothor/MusicScoreManager/releases/latest")
      .then(function (r) { return r.ok ? r.json() : null; })
      .then(function (data) {
        if (data && data.tag_name) {
          for (var j = 0; j < sessionStorage.length; j++) {
            var k = sessionStorage.key(j);
            if (k && k.indexOf("__source") !== -1) {
              try {
                var v = JSON.parse(sessionStorage.getItem(k)) || {};
                v.version = data.tag_name;
                sessionStorage.setItem(k, JSON.stringify(v));
              } catch (e) {}
            }
          }
          syncBadge(data.tag_name);
        }
      })
      .catch(function () {});
  } catch (err) {}
})();
