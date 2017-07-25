document.addEventListener('DOMContentLoaded', onDocumentReady, false);
window.addEventListener('popstate', reloadPageOnBackButtonPress);

function onDocumentReady() {
    buildObjectListPanel();
}

function reloadPageOnBackButtonPress(event) {
    location.reload();
}

function postConnectionString() {
    var xhr = new XMLHttpRequest();
    xhr.open('POST', '/connect');
    xhr.setRequestHeader('Content-Type', 'application/json');
    xhr.onreadystatechange = function () {
        if (xhr.readyState === 4) {
            if (xhr.status === 200) {
                location.reload();
            } else if (xhr.status === 406) {
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
    var range;
    if (document.selection) { 
        range = document.body.createTextRange();
        range.moveToElementText(document.getElementById(containerid));
        range.select().createTextRange();
    } else if (window.getSelection) {
         range = document.createRange();
         range.selectNode(document.getElementById(containerid));
         window.getSelection().removeAllRanges();
         window.getSelection().addRange(range);
    }
}

var currentObjectList;

function buildObjectListPanel() {
    var comboBoxItems = allServerObjects.map(function (objList) {
        return "<option value='" + objList.type_desc + "'>" + objList.type_desc + "</option>";
    });
    document.getElementById("objectTypeComboBox").innerHTML = comboBoxItems.join('');
    objectTypeComboBoxChange();
}

function objectTypeComboBoxChange() {
    var comboBox = document.getElementById("objectTypeComboBox");
    var sel_type_desc = comboBox.options[comboBox.selectedIndex].value;
    comboBox.setAttribute("title", sel_type_desc);
    currentObjectList = allServerObjects.filter(function (obj) {
        return obj.type_desc === sel_type_desc;
    })[0];

    buildObjectListView(document.getElementById("objFilter").value);
}

function buildServerObjectLink(objectName) {
    return "<a href=" + location.protocol + "//" + location.host + "/?" + objectNameParam + "=" + objectName +
        " onclick='getObjText(event)' title='" + objectName + "'>" + objectName + "</a>";
}

function getObjText(event) {
    var link = event.target;
    var xhr = new XMLHttpRequest();
    xhr.open("GET", "/objtext?" + objectNameParam + "=" + link.textContent);
    xhr.setRequestHeader("Content-Type", "application/json");
    xhr.onreadystatechange = function () {
        if (xhr.readyState === 4) {
            if (xhr.status === 200) {
                document.getElementById("text").innerHTML = xhr.response;
                history.pushState({}, link.textContent, "?" + objectNameParam + "=" + link.textContent);
            }
        }
    };
    xhr.send();
    event.preventDefault();
}

function onObjFilterChange(event) {
    var filter = event.target.value;
    if (filter === "") {
        objectTypeComboBoxChange();
        return;
    }

    buildObjectListView(filter);
}

function buildObjectListView(filter) {
    var objectList;
    if (filter !== "") {
        objectList = currentObjectList.objects.filter(function (obj) {
            return obj.toUpperCase().indexOf(filter.toUpperCase()) !== -1;
        });
    } else {
        objectList = currentObjectList.objects;
    }

    document.getElementById("objectList").innerHTML =
        objectList.map(function (obj) {
            return "<li>" + buildServerObjectLink(obj) + "</li>";
        }).join('');
}

function serverOnBlur(event) {
    if (document.getElementById("server").value === "") {
        return;
    }
    var xhr = new XMLHttpRequest();
    xhr.open("POST", "/databaselist");
    xhr.setRequestHeader("Content-Type", "application/json");
    xhr.onreadystatechange = function () {
        if (xhr.readyState === 4) {
            if (xhr.status === 200) {
                var databaseList = JSON.parse(xhr.response);
                document.getElementById("database").innerHTML =
                    databaseList.map(function (dbName) {
                        return "<option value='" + dbName + "'>" + dbName + "</option>";
                    }).join('');
                conn = getCurrentConnectionInfo();
                if (conn !== null) {
                    document.getElementById("database").value = conn.database;
                }
                document.getElementById("errorMessage").innerHTML = "";
                document.getElementById("btConnect").disabled = false;
                document.getElementById("btCancel").disabled = false;
                document.getElementById("server").disabled = false;
                document.getElementById("database").disabled = false;
            } else if (xhr.status === 406) {
                var error = JSON.parse(xhr.response);
                document.getElementById("errorMessage").innerHTML = error.errorMessage;
                document.getElementById("btConnect").disabled = false;
                document.getElementById("btCancel").disabled = false;
                document.getElementById("server").disabled = false;
                document.getElementById("database").disabled = false;
                document.getElementById("database").innerHTML = "";
            }
        }
    };
    if (this.serverName !== document.getElementById("server").value) {
        this.serverName = document.getElementById("server").value;
        document.getElementById("btConnect").disabled = true;
        document.getElementById("btCancel").disabled = true;
        document.getElementById("server").disabled = true;
        document.getElementById("database").innerHTML = "<option>Retrieving database list</option>";
        document.getElementById("database").disabled = true;
        xhr.send(JSON.stringify({
            server: document.getElementById("server").value
        }));
    }
}

function getCurrentConnectionInfo() {
    var paramsText = document.getElementById("connectionString").textContent;    
    if (paramsText !== "") {
        return JSON.parse(paramsText);
    } else {
        return null;
    }
}

function openModal() {
    conn = getCurrentConnectionInfo();
    if (conn !== null) {
        document.getElementById("server").value = conn.server;
        serverOnBlur(null);
    }
    var overlay = document.getElementById('overlay');
    overlay.classList.remove("is-hidden");
}

function closeModal() {
    document.getElementById("errorMessage").innerHTML = "";
    var overlay = document.getElementById('overlay');
    overlay.classList.add("is-hidden");
}