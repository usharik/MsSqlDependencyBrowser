function openModal() {
    var overlay = document.getElementById('overlay');
    overlay.classList.remove("is-hidden");
}

function closeModal() {
    var overlay = document.getElementById('overlay');
    overlay.classList.add("is-hidden");
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

var MsSqlDependencyBrowser = angular.module('MsSqlDependencyBrowser', ['ngRoute', 'ngCookies']);

MsSqlDependencyBrowser.config(function ($routeProvider, $locationProvider, $sceProvider) {
    $sceProvider.enabled(false);
    $routeProvider
        .when('/:schemaname.:objectname', {
            templateUrl: 'objectText.html',
            controller: 'ObjectTextCtrl'
        })
        .otherwise({
            redirectTo: '/'
        });
});

MsSqlDependencyBrowser.service('DatabaseService', function ($rootScope, $http, $cookies) {
    var server = $cookies.get('server') ? $cookies.get('server') : '';
    var database = $cookies.get('database') ? $cookies.get('database') : '';
    var databaseList = [];

    this.setConnectData = function (_server, _database, _databaseList) {
        server = _server;
        $cookies.put('server', server);
        database = _database;
        $cookies.put('database', database);
        databaseList = _databaseList;
    };

    this.getServer = function () {
        return server;
    };

    this.getDatabase = function () {
        return database;
    };

    this.getDatabaseList = function () {
        return databaseList;
    };

    this.isConnected = function () {
        return server !== '' && database !== '';
    };
});

MsSqlDependencyBrowser.controller('ConnectFormCtrl', function ($scope, $rootScope, $http, DatabaseService) {

    $scope.model = {
        server: DatabaseService.getServer(),
        database: DatabaseService.getDatabase(),
        databaseList: [],
        inProgress: false,
        errorMessage: ''
    };

    if (DatabaseService.isConnected()) {
        loadDatabaseList();
    }

    function loadDatabaseList() {
        $scope.model.inProgress = true;
        $http
            .post('/databaselist', { 'server': $scope.model.server })
            .then(function successCallback(response) {
                $scope.model.databaseList = response.data;
                $scope.model.database = $scope.model.databaseList.indexOf($scope.model.database) > -1 ? $scope.model.database : response.data[0];
                $scope.model.inProgress = false;
            }, function errorCallback(response) {
                $scope.model.errorMessage = response.data.errorMessage;
                $scope.model.databaseList = [];
                $scope.model.inProgress = false;
            });
    }

    $scope.blur = function () {
        if ($scope.model.server.trimLeft().trimRight() === '') {
            return;
        }
        loadDatabaseList();
    };

    $scope.connect = function () {
        $scope.model.inProgress = true;
        $http
            .post('/testconnect', { 'server': $scope.model.server, 'database': $scope.model.database })
            .then(function successCallback(response) {
                $scope.model.inProgress = false;
                DatabaseService.setConnectData($scope.model.server, $scope.model.database, $scope.model.databaseList);
                $rootScope.$broadcast('connectedSuccessful');
                closeModal();
            }, function errorCallback(response) {
                $scope.model.errorMessage = response.data.errorMessage;
                $scope.model.inProgress = false;
            });
    };
});

MsSqlDependencyBrowser.controller('HeaderCtrl', function ($scope, $rootScope, $routeParams, DatabaseService) {

    if (DatabaseService.isConnected()) {
        $scope.connectionInfo = 'Server: ' + DatabaseService.getServer() + '; Database: ' + DatabaseService.getDatabase();
    } else {
        $scope.connectionInfo = '';
    }

    $rootScope.$on('connectedSuccessful', function () {
        $scope.connectionInfo = 'Server: ' + DatabaseService.getServer() + '; Database: ' + DatabaseService.getDatabase();        
    });

    $rootScope.$on('newObject', function () {
        $scope.objectName = $routeParams.objectname;
    });

    $scope.connectDialog = function () {
        openModal();
    };
});

MsSqlDependencyBrowser.controller('ObjectNavigatorCtrl', function ($scope, $rootScope, $http, $cookies, DatabaseService) {

    var serverObjectList = [];

    $scope.objectTypeList = [];
    $scope.objectType = null;
    $scope.currentObjectList = [];
    $scope.objectText = '';

    if (DatabaseService.isConnected()) {
        loadObjectList(null, null);
    }

    $scope.objectTypeChange = function () {
        $scope.currentObjectList = serverObjectList.filter(function (obj) {
            return obj.type_desc === $scope.objectType;
        })[0].objects;
        $cookies.put('objectType', $scope.objectType);
    };

    $scope.objectLinkClick = function (event) {
        DatabaseService.setObjectName(event.target.textContent);
        loadObjectText();
        event.preventDefault();
    };

    function loadObjectList(event, data) {
        $http
            .post('/serverobjectlist', { 'server': DatabaseService.getServer(), 'database': DatabaseService.getDatabase() })
            .then(function successCallback(response) {
                serverObjectList = response.data;
                $scope.objectTypeList = [];
                serverObjectList.forEach(function (objList) {
                    $scope.objectTypeList.push(objList.type_desc);
                });
                $scope.objectType = $cookies.get('objectType') ? $cookies.get('objectType') : $scope.objectTypeList[0];
                $scope.objectTypeChange();
            }, function errorCallback(response) {
                console.log(response);
            });
    }

    $rootScope.$on('connectedSuccessful', loadObjectList);
});

MsSqlDependencyBrowser.controller('ObjectTextCtrl', function ($scope, $rootScope, $http, $routeParams, DatabaseService) {
    $scope.objectText = '';
    $scope.objectName = $routeParams.objectname;

    function loadObjectText() {
        $rootScope.$broadcast('newObject');
        $http
            .post('/objtext?sch=' + $routeParams.schemaname + '&obj=' + $routeParams.objectname, { 'server': DatabaseService.getServer(), 'database': DatabaseService.getDatabase() })
            .then(function successCallback(response) {
                $scope.objectText = response.data;
            }, function errorCallback(response) {
                console.log(response);
                $scope.objectText = 'Application not responding';
            });
    }

    $rootScope.$on('connectedSuccessful', loadObjectText);
    loadObjectText();
});