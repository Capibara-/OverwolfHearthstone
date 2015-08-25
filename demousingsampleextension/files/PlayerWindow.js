﻿
// Init Bootstrap tooltips:
$(document).ready(function () {
    $('[data-toggle="tooltip"]').tooltip();
});

function addImageToBar(path) {
    addImageToBarWithTooltip("playerSide", path, "No info.", "cardThumbnail");
};

function addImageToBarWithTooltip(elementName, path, tooltipTitle, id) {
    console.log(tooltipTitle);
    // TODO: Validate input.
    $('#' + elementName).prepend('<div class="thumbnail" data-toggle="tooltip" data-placement="bottom" title=\"' + tooltipTitle + '\" id=\"' + id + '\"> <img src=\"' + path + '\" class="img-responsive"></div>');
    $('[data-toggle="tooltip"]').tooltip();
};


function dragResize(edge) {
    overwolf.windows.getCurrentWindow(function (result) {
        if (result.status == "success") {
            overwolf.windows.dragResize(result.window.id, edge);
        }
    });
};

function dragMove() {
    overwolf.windows.getCurrentWindow(function (result) {
        if (result.status == "success") {
            overwolf.windows.dragMove(result.window.id);
        }
    });
};

function closeWindow() {
    overwolf.windows.getCurrentWindow(function (result) {
        if (result.status == "success") {
            overwolf.windows.close(result.window.id);
        }
    });
};

function takeScreenshot() {
    overwolf.media.takeScreenshot(function (result) {
        if (result.status == "success") {
            var imagePath = result.url;
            console.log(result.url);
            addImageToBar(result.url);
        }
    });
};

var sampleLibraryObj = null;

function initLibrary() {
    if (typeof (overwolf) != "undefined") {
        overwolf.games.getRunningGameInfo(function (gameInfoOBject) {
            console.log(gameInfoOBject);
            var isGameRunning = false;
            if (gameInfoOBject != null) {
                console.log("Game is running!");
                isGameRunning = true;
            }
            overwolf.extensions.current.getExtraObject("NinjaLibrary", function (result) {
                console.log("startup " + result.status);
                if (result.status == "success") {
                    sampleLibraryObj = result.object;
                    // Init C# module:
                    sampleLibraryObj.CardHandEvent.addListener(cardHandEventFired);
                    sampleLibraryObj.CardPlayedEvent.addListener(cardPlayedEventFired);
                    sampleLibraryObj.OpponentCardPlayedEvent.addListener(onOpponentCardPlay)
                    sampleLibraryObj.Init(genericCallback);
                    if (isGameRunning) {
                        sampleLibraryObj.GameOn(genericCallback);
                    }
                    else {
                        sampleLibraryObj.GameOff(genericCallback);
                    }
                }
            });
        });
    }
};

function onOpponentCardPlay(result) {
    var card = JSON.parse(result.CardJSON);
    console.log("Opponent moved " + card.Name + " from hand to table.");
    var path = "Images_renamed/" + card.ID + ".png";
    addImageToBarWithTooltip("opponentSide", path, card.Name, card.ID);
    $('#' + card.ID).css('opacity', '0.5');
}

function genericCallback(result) {
    console.log("genericCallback: " + result);
};

function cardPlayedEventFired(result) {
    var Card = JSON.parse(result.CardJSON);
    console.log("Player moved " + Card.Name + " from hand to table.");
    if ($('#' + Card.ID).length == 0) {
        //card does not exist insert it to table and then change border
        cardHandEventFired(result);
    }
    $('#' + Card.ID).css('opacity', '0.5');
};



function cardHandEventFired(result) {
    var Card = JSON.parse(result.CardJSON);
    console.log("Player received " + Card.Name + " from deck.");
    var path = "Images_renamed/" + Card.ID + ".png";
    addImageToBarWithTooltip("playerSide", path, Card.Name, Card.ID);
};

function onGameInfoUpdate(gameInfoChangeDataObject) {
    if (gameInfoChangeDataObject.gameChanged) {
        console.log("onGameInfoUpdate: Game changed.");
        return;
    }

    if (gameInfoChangeDataObject.resolutionChanged) {
        console.log("onGameInfoUpdate: Resolution changed.");
        return;
    }
};

window.onload = function (e) {
    initLibrary();
    overwolf.games.onGameInfoUpdated.addListener(onGameInfoUpdate);
    //initSize();
};

function initSize() {
    overwolf.games.onGameLaunched.addListener(
        function (gameInfoObject) {
            alert("width: " + gameInfoObject.width + " height: " + gameInfoObject.height);
            $('#container').width(gameInfoObject.width);
            $('#container').height(gameInfoObject.height);
            overwolf.windows.changeSize("PlayerWindow", gameInfoObject.width, gameInfoObject.height, function () { console.log('Window size changed.') });
        });
};

function reloadPage() {
    location = location;
}

function openWindow(windowName) {
    overwolf.windows.obtainDeclaredWindow(windowName, function (result) {
        if (result.status == "success") {
            overwolf.windows.restore(result.window.id, function (result) {
                console.log(result);
            });
        }
    });
};

function resizeWindow() {
    var height = parseInt($('#heightText').val());
    var width = parseInt($('#widthText').val());
    overwolf.windows.getCurrentWindow(function (result) {
        if (result.status == "success")
        {
            overwolf.windows.changeSize(result.window.id, width, height, genericCallback);
            $('#container').height(height);
            $('#container').width(width);
        }
    });
}