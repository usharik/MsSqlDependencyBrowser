function postConnectionString() {
    var xhr = new XMLHttpRequest();
    xhr.open('POST', '/connect');
    xhr.setRequestHeader('Content-Type', 'application/json')
    xhr.onreadystatechange = function () {
        if (xhr.readyState == 4) {
            if (xhr.status == 200) {
                location.reload();
            } else if (xhr.status == 406) {
                document.getElementById("errorMessage").innerHTML = "Connection error.";
                document.getElementById("btConnect").disabled = false;
                document.getElementById("btCancel").disabled = false;
            }
        }
    };
    document.getElementById("btConnect").disabled = true;
    document.getElementById("btCancel").disabled = true;
    
    xhr.send(JSON.stringify({
        server: document.getElementById("server").value,
        database: document.getElementById("database").value
    }));
}