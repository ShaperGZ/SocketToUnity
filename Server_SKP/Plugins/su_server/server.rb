require 'sketchup.rb'
require 'socket'

#UI
module Sketchup::ServerTools
    toolbar1 = UI::Toolbar.new "Connect"
    cmd1 = UI::Command.new("Connect"){ToConnect()}
    cmd1.small_icon = "Images/connect.png"
    cmd1.large_icon = "Images/connect.png"
    cmd1.tooltip = "Connect"
    cmd1.status_bar_text = "与Unity建立连接"
    cmd1.menu_text = "Connect"
    toolbar1 = toolbar1.add_item cmd1
    toolbar1.show

    toolbar2 = UI::Toolbar.new "ReConnect"
    cmd2 = UI::Command.new("ReConnect"){ToReConnect()}
    cmd2.small_icon = "Images/reconnect.png"
    cmd2.large_icon = "Images/reconnect.png"
    cmd2.tooltip = "ReConnect"
    cmd2.status_bar_text = "重新连接到Unity"
    cmd2.menu_text = "ReConnect"
    toolbar2 = toolbar2.add_item cmd2
    toolbar2.show

    toolbar3 = UI::Toolbar.new "Close"
    cmd3 = UI::Command.new("Close"){ToClose()}
    cmd3.small_icon = "Images/close.png"
    cmd3.large_icon = "Images/close.png"
    cmd3.tooltip = "Close"
    cmd3.status_bar_text = "关闭连接"
    cmd3.menu_text = "Close"
    toolbar3 = toolbar3.add_item cmd3
    toolbar3.show

    toolbar4 = UI::Toolbar.new "SendAllGroup"
    cmd4 = UI::Command.new("SendAllGroup"){ToSendAllGroup()}
    cmd4.small_icon = "Images/sendall.png"
    cmd4.large_icon = "Images/sendall.png"
    cmd4.tooltip = "SendAllGroup"
    cmd4.status_bar_text = "发送场景中的所有组"
    cmd4.menu_text = "SendAllGroup"
    toolbar4 = toolbar4.add_item cmd4
    toolbar4.show
end

def ToConnect
    $serverRe = Server.new
    $serverRe.connect("127.0.0.1",8088)
    $serverRe.waitClient()
end

def ToReConnect
    $serverRe.reConnect()
end

def ToClose
    $serverRe.close()
end

def ToSendAllGroup
    SendAllGroup()
end

class Server
    def connect(ip,port)
        $server= TCPServer.open(ip,port)
    end

    def waitClient
        puts "等待客户端连接。。。"
        $client = $server.accept
        puts "客户端连接成功！"
    end

    def reConnect
        puts "重新连接"
        #close()
        waitClient()
    end

    def sendMessage(s)
        $client.puts "    "+s+"!"
    end

    def recieveMessage()
        puts "等待接收。。。"
        m = $client.gets.chomp
        puts m
        puts "接收完成！"
    end

    def close()
        $client.close
    end
end

def DeleteObject(entity)
    puts entity.guid
    message = "D|"+entity.guid
    $serverRe.sendMessage(message)
end

def UpdateObject(tarGroup)
    #获取ID
    id = tarGroup.guid
    #获取组名
    name = tarGroup.name
    #获取高度(m)
    box = tarGroup.local_bounds
    height = box.depth
    @isGeometry = false
    matrix=tarGroup.transformation.to_a
    height = (height * matrix[10])/39.37007874015748
    faceList = Array.new
    #获取底面各点坐标(m)
    tarGroup.entities.each {|entity|
        if entity.typename == "Face"
            faceList<<entity
            if entity.normal.z == -1
                @isGeometry = true
                @normalFace = entity
                vertices = @normalFace.vertices
                @ve = ""
                vertices.each {|dot|
                    pt = Geom::Point3d.new(dot.position.x,dot.position.y,dot.position.z)
                    pos = tarGroup.transformation * pt
                    @ve += "|"+pos.x.to_s+","+pos.z.to_s+","+pos.y.to_s
                }
                @message1 = "U|"+id.to_s+"|"+name+"|"+height.to_s+"|"+tarGroup.transformation.xaxis.to_s+@ve
            end
        end
    }

    @va = ""
    @message2 = "F|"+id.to_s+"|"+name+"|"
    puts faceList.size
    faceList.each{|faceV|
        vs = faceV.vertices
        vs.each{|dot|
            ps = Geom::Point3d.new(dot.position.x,dot.position.y,dot.position.z)
            pos = tarGroup.transformation * ps
            @va += pos.x.to_s+","+pos.z.to_s+","+pos.y.to_s+"_"
        }
        @message2 += @va+"|"
        @va = ""
    }

    if name == "face"   #当组的名字为Face时，发送所有Face上的所有点
        puts @message2
        $serverRe.sendMessage(@message2)
    elsif @isGeometry == true
        puts @message1
        $serverRe.sendMessage(@message1)
    end
end

def SendAllGroup
    entites = Sketchup.active_model.entities
    entites.each {|entity|
        if entity.typename == "Group"
            UpdateObject(entity)
        end
        sleep(0.01)
    }
end

#打开文件或新建时，为每一个图元加载一个监听器
class MyAppObserver < Sketchup::AppObserver
    def onOpenModel(model)
        puts "打开文件"
        entites = Sketchup.active_model.entities
        entites.add_observer(NewEntityObserver.new)
        entites.each {|entity|
            if entity.typename == "Group"
                entity.add_observer(GroupObserver.new)
            end
        }
    end

    def onNewModel(model)
        puts "新建场景"
        Sketchup.active_model.entities.add_observer(NewEntityObserver.new)
    end
end
class NewEntityObserver < Sketchup::EntitiesObserver
    def onElementAdded(entities, entity)       #当添加一个Entities时调用
        puts "onElementModified: #{entity} type="+entity.typename
        if entity.typename == "Group"
            UpdateObject(entity)
            entity.add_observer(GroupObserver.new)
        end
    end
end
class GroupObserver < Sketchup::EntityObserver
    def onChangeEntity(entity)
        puts "已改变组"
        if entity.entities.size != nil
            UpdateObject(entity)
        else
            DeleteObject(entity)
        end
    end
end
#为现场景的所有图元加载一个监听器
Sketchup.active_model.entities.add_observer(NewEntityObserver.new)
Sketchup.add_observer(MyAppObserver.new)
