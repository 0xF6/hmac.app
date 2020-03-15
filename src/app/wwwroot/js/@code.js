function domReady() {
    window.editor = ace.edit("editor");
    editor.renderer.setShowGutter(false);
    editor.getSession().setUseWorker(false);
    editor.setTheme("ace/theme/chrome");
    editor.getSession().setMode("ace/mode/json");
}

function setValue(id, val) {
    $(id).val(val);
}

function castHMAC(adapter, text, secret) {
    if (adapter === "SHA512")
        return CryptoJS.HmacSHA512(text, secret).toString();
    if (adapter === "SHA256")
        return CryptoJS.HmacSHA256(text, secret).toString();
    if (adapter === "SHA384")
        return CryptoJS.HmacSHA384(text, secret).toString();
    if (adapter === "SHA1")
        return CryptoJS.HmacSHA1(text, secret).toString();
    throw new Error("not valid adapter id: " + adapter);
}