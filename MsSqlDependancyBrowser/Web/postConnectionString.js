function postConnectionString() {
    var xhr = new XMLHttpRequest();
    xhr.open('POST', '/connect');
    xhr.setRequestHeader('Content-Type', 'application/json')
    xhr.onreadystatechange = function () {
        if (xhr.readyState == 4) {
            if (xhr.status == 200) {
                location.reload();
            } else if (xhr.status == 406) {
                var error = JSON.parse(xhr.response);
                document.getElementById("errorMessage").innerHTML = error.errorMessage;
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

function selectAllText(containerid) {
    if (document.selection) { 
        var range = document.body.createTextRange();
        range.moveToElementText(document.getElementById(containerid));
        range.select().createTextRange();
    } else if (window.getSelection) {
        var range = document.createRange();
         range.selectNode(document.getElementById(containerid));
         window.getSelection().removeAllRanges();
         window.getSelection().addRange(range);
    }
}