'use strict';
app.controller('ChatsController', [
    '$scope', '$sce', 'chatsHubService', 'chatsService', 'localStorageService',
    function($scope, $sce, chatsHubService, chatsService, localStorageService) {
        $scope.currentChat = {};
        //var currentChat = {};
        $scope.messages = [];
        chatsHubService.start();
        $scope.currentUserName = localStorageService.get("authorizationData").userName;
        // Click event-handler for broadcasting chat messages.
        chatsService.getChats().then(function(result) {
            $scope.chats = result.data;
            if (result.data[0] != null) {
                $scope.currentChat = result.data[0];
                //currentChat = result.data[0];
                $scope.getMessagesForChat($scope.currentChat);
            }

        });
        $('#submitButton').click(function() {
            // Call Server method.
            var message = {};
            message.isFavourite = false;
            message.isRead = false;
            //var messageText = $('texxt').val();;
            var messageText = $('texxt').val();;
            //message.text = $('#texxt').val();
            message.text = $('#texxt').val();
            message.chat = {};
            message.chat.id = $scope.currentChat.id;
            chatsHubService.sendMessage(message);


            $('#texxt').val("");
        });
        $('#addChat').click(function() {
            // Call Server method.
            chatsService.addChat($('#chatTitle').val(), $('#chatIsPrivate').is(":checked")).then(function(obj) {
                chatsService.getChats().then(function(result) {
                    $scope.chats = result.data;
                });
            });
            $('#texxt').val("");
        });

        $('#addUserToChat').click(function() {
            // Call Server method.
            chatsService.addUserToChat($('#chatName').val(), $('#userNamee').val()).then(function(obj) {
                chatsService.getChats().then(function(result) {
                    $scope.chats = result.data;
                });
            });

            $('#chatName').val("");
            $('#userNamee').val("");

        });

        $.connection.hub.start()
            .done(function(data) {
                if (data && data.token) {
                    chatsHubService.setTokenCookie(data.token);

                }
            });

        $scope.getMessagesForChat = function(chat) {
            $scope.currentChat = chat;
            //currentChat = chat;
            chatsService.getMessagesForChat(chat.id)
                .then(function(result) {
                    $scope.messages = result;
                    //prettyPrint();
                });
        };

        $scope.deleteMessage = function(id) {
            for (var i = 0; i < $scope.messages.length; i++) {
                if ($scope.messages[i].id == id) {
                    if ($scope.messages[i].user.userName != $scope.currentUserName) {
                        alert("You can delete only your own messages");
                        return;
                    }
                }
            }
            chatsHubService.deleteMessage(id);

        };

        function appendMessage(userName, message) {
            var newMessageSpan = $("<li>").text(userName + ": " + message);
            newMessageSpan.appendTo(angular.element("#messages"));
        }

        /*$scope.sendMessage = function(message) { //, user, chatName) {
            appendMessage("andrey", message);
            chatsHubService.sendMessage(message); //, user, chatName);
        }*/

        chatsHubService.messageCallback = function(message) {
            $scope.getMessagesForChat($scope.currentChat);

        }
        $scope.getClass = function(index) {
            if ($scope.messages[index].user.userName == $scope.currentUserName)
                return "i";
            else
                return "friend-with-a-SVAGina";
        }


        //for code insertion part
        /*document.getElementById("paste").onclick = function() {
            document.getElementById('test').focus();
            pasteHtmlAtCaret();
            return false;
        };*/


        function getInputSelection(el) {
            var start = 0,
                end = 0,
                normalizedValue,
                range,
                textInputRange,
                len,
                endRange;

            if (typeof el.selectionStart == "number" && typeof el.selectionEnd == "number") {
                start = el.selectionStart;
                end = el.selectionEnd;
            } else {
                range = document.selection.createRange();

                if (range && range.parentElement() == el) {
                    len = el.value.length;
                    normalizedValue = el.value.replace(/\r\n/g, "\n");

                    // Create a working TextRange that lives only in the input
                    textInputRange = el.createTextRange();
                    textInputRange.moveToBookmark(range.getBookmark());

                    // Check if the start and end of the selection are at the very end
                    // of the input, since moveStart/moveEnd doesn't return what we want
                    // in those cases
                    endRange = el.createTextRange();
                    endRange.collapse(false);

                    if (textInputRange.compareEndPoints("StartToEnd", endRange) > -1) {
                        start = end = len;
                    } else {
                        start = -textInputRange.moveStart("character", -len);
                        start += normalizedValue.slice(0, start).split("\n").length - 1;

                        if (textInputRange.compareEndPoints("EndToEnd", endRange) > -1) {
                            end = len;
                        } else {
                            end = -textInputRange.moveEnd("character", -len);
                            end += normalizedValue.slice(0, end).split("\n").length - 1;
                        }
                    }
                }
            }

            return {
                start: start,
                end: end
            };
        }

        function replaceSelectedText(el) {
            var sel = getInputSelection(el), val = el.value;
            el.value = [val.slice(0, sel.start), " [CODE] ", val.slice(sel.start)].join('');
            val = el.value;
            el.value = [val.slice(0, sel.end + 6), " [!CODE] ", val.slice(sel.end + 6)].join('');
            //val.slice(0, sel.start) + text + val.slice(sel.end);
        }


        $scope.pasteHtmlAtCaret = function() {
            var el = document.getElementById("texxt");
            replaceSelectedText(el);


            /*document.getElementById('texxt').focus();
            var sel, range;
            if (window.getSelection) {
                // IE9 and non-IE
                sel = window.getSelection();
                if (sel.getRangeAt && sel.rangeCount) {
                    range = sel.getRangeAt(0);

                    // Range.createContextualFragment() would be useful here but is
                    // only relatively recently standardized and is not supported in
                    // some browsers (IE9, for one)
                    var el = document.createElement("div");
                    //var el = document.getElementById('test');


                    //working example
                    el.innerHTML = "[CODE]" + sel.toString() + '[!CODE] </br>';

                    //may be required to recode the coded text
                    //element.classList.remove.("prettyprinted");

                    range.deleteContents();

                    var frag = document.createDocumentFragment(), node, lastNode;
                    while ((node = el.firstChild)) {
                        lastNode = frag.appendChild(node);
                    }
                    var firstNode = frag.firstChild;
                    range.insertNode(frag);
                    prettyPrint();
                    // Preserve the selection
                    if (lastNode) {
                        range = range.cloneRange();
                        range.setStartAfter(lastNode);
                        range.setStartBefore(firstNode);

                        //use this to delete selection after insertion
                        //range.collapse(true);

                        sel.removeAllRanges();
                        sel.addRange(range);
                    }
                }
            } else if ((sel = document.selection) && sel.type != "Control") {
                // IE < 9
                var originalRange = sel.createRange();
                originalRange.collapse(true);
                sel.createRange().pasteHTML(html);

                //delete this to delete selection after insertion
                range = sel.createRange();
                range.setEndPoint("StartToStart", originalRange);
                range.select();
            }*/
        }


        $scope.prettiFy = function(message) {
            var el = angular.element('#messageDiv');
            if (message.indexOf("[CODE]") !== -1 && message.indexOf("[!CODE]") !== -1) {
                var start = el.innerHtml.indexOf("[CODE]");
                var end = el.innerHtml.indexOf("[!CODE]");
            }
        }

    }

]);