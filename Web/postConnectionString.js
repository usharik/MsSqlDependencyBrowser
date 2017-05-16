function postConnectionString() {
    var xhr = new XMLHttpRequest();
    xhr.open('POST', '/connect');
    xhr.setRequestHeader('Content-Type', 'application/text')
    xhr.onreadystatechange = function () {
        if (xhr.readyState == 4) {
            if (xhr.status == 200) {
                location.reload();
            }
        }
    };
    document.getElementById("btConnect").innerHTML = "Wait for connection";
    document.getElementById("btConnect").disabled = true;
    xhr.send(document.getElementById("connectionString").value);
}