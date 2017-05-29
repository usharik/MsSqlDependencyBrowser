function openModal() {
    var paramsText = document.getElementById("connectionString").textContent;
    if (paramsText != "") {
        var params = JSON.parse(paramsText);
        document.getElementById("server").value = params.server;
        document.getElementById("database").value = params.database;
    }
    var overlay = document.getElementById('overlay');
    overlay.classList.remove("is-hidden");
}

function closeModal() {
    var overlay = document.getElementById('overlay');
    overlay.classList.add("is-hidden");
}