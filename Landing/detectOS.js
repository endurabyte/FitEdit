async function detectOS() {
  var platform = window.navigator.platform;

  if (/Mac/.test(platform)) {

    var link = document.getElementById('mac-x64-download-link');
    var link2 = document.getElementById('mac-arm-download-link');
    var text = document.getElementById('mac-help-text');
    link.style.display = 'block';
    link2.style.display = 'block';
    text.style.display = 'block';
  } else if (/Win/.test(platform)) {
    var link = document.getElementById('win-download-link');
    link.style.display = 'block';
  }
}
