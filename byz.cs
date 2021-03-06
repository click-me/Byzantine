using System;
using System.Collections;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Description;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


[ServiceBehavior(IncludeExceptionDetailInFaults = true,
    InstanceContextMode = InstanceContextMode.PerCall,
    ConcurrencyMode = ConcurrencyMode.Multiple)] // Reentrant)] 
public class ByzService : INodeService
{


    public Message[] Messages(Message[] imsgs)
    {
        List<Message> outputList = new List<Message>();
        int time = imsgs[0].Time;

        foreach (var msg in imsgs)
        {

            //第一次发送
            if (msg.From == 0 && byz.IsFaulty == 0)
            {
               
                for (int i = 1; i <= byz.MaxIndex; i++)
                {
                    outputList.Add(new Message(msg.Time, byz.Index, i, Convert.ToString(byz.Init)));
                }

                return outputList.ToArray();
              
               
            }
            if (msg.From == 0 && byz.IsFaulty == 1)
            {

                Console.WriteLine(byz.FileName);
                string[] lines = System.IO.File.ReadAllLines(byz.FileName);
                string line = lines[imsgs[0].Time];
                string[] outputs = line.Split(' ');
                for (int i = 1; i <= byz.MaxIndex; i++)
                {
                    Console.WriteLine($"{imsgs[0].Time}, {byz.Index}, {i}, {outputs[i - 1]}");
                    outputList.Add(new Message(imsgs[0].Time, byz.Index, i, outputs[i - 1]));
                }
                return outputList.ToArray();
            }

            //每次都会收到四个消息
            //更新EIG
            int index = 0;
            var Keys = new List<string>(byz.EIG.Keys);
            foreach (string key in Keys)
            {
                if (key.Length == msg.Time && key[key.Length - 1].ToString().Equals(msg.From.ToString()))
                {
                    byz.EIG[key] = int.Parse(msg.Msg[index].ToString());
                    index++;
                }
            }
        }
        //发送阶段
        if (time < byz.MaxLevel)
        {
            //发送新消息
            if (byz.IsFaulty == 0)
            {
                string m = "";
                foreach (var key in byz.EIG.Keys)
                {
                    if (key.Length == imsgs[0].Time && !key.Contains(byz.Index.ToString()) && !key.Equals("λ"))
                    {
                        m = m + byz.EIG[key].ToString();
                    }
                }
                for (int i = 1; i <= byz.MaxIndex; i++)
                {
                    outputList.Add(new Message(imsgs[0].Time, byz.Index, i, m));
                }

            }
            //叛徒
            else
            {
                string[] lines = System.IO.File.ReadAllLines(byz.FileName);
                string line = lines[imsgs[0].Time];
                string[] outputs = line.Split(' ');
                for (int i = 1; i <= byz.MaxIndex; i++)
                {
                    outputList.Add(new Message(imsgs[0].Time, byz.Index, i, outputs[i - 1]));
                }
            }


        }
        
        
        //判断阶段
        else
        {
            Evaluate();
            SendToArc();
            outputList.Add(new Message(imsgs[0].Time, byz.Index, 0, "Finish"));
        }

        return outputList.ToArray();
    }

    public void Evaluate()
    {
        //复制底层
        foreach (var key in byz.EIG.Keys)
        {
            if (key.Length == byz.MaxLevel)
            {
                if (byz.EIG[key] != 2)
                {
                    byz.EIG_eva[key] = byz.EIG[key];
                }
                else {
                    byz.EIG_eva[key] = byz.V0;
                }
            }
        }

        VoteResult("");
        print();
    }

    public int VoteResult(string str)
    {
        if (str.Length == byz.MaxLevel)
        {
            //Console.WriteLine($"{str}: {byz.EIG_eva[str]}");
            return byz.EIG_eva[str];
        }
        else
        {
            int sum0 = 0;
            int sum1 = 0;
            int sum2 = 0;
            for (int i = 1; i <= byz.MaxIndex; i++)
            {
                string childKey = str + i;
                if (byz.EIG_eva.ContainsKey(childKey))
                {
                    int childResult = VoteResult(childKey);
                    if (childResult == 0)
                    {
                        sum0++;
                    }
                    if (childResult == 1)
                    {
                        sum1++;
                    }
                    if (childResult == 2)
                    {
                        sum2++;
                    }
                }
            }

            int max = Math.Max(Math.Max(sum0, sum1), sum2);
            if (sum0 == max && sum0 > Math.Max(sum1, sum2))
            {
                //Console.WriteLine($"{str}: 0");
                byz.EIG_eva[str] = 0;
                return 0;
            }
            if (sum1 == max && sum1 > Math.Max(sum0, sum2))
            {
                //Console.WriteLine($"{str}: 1");
                byz.EIG_eva[str] = 1;
                return 1;
            }
            if (sum2 == max && sum2 > Math.Max(sum0, sum1))
            {
                //Console.WriteLine($"{str}: 2");
                byz.EIG_eva[str] = 2;
                return 2;
            }
            //Console.WriteLine($"{str}: {byz.V0}");
            byz.EIG_eva[str] = byz.V0;
            return byz.V0;

        }
    }

    public void print() {
        Console.WriteLine($"0 {byz.Index} {byz.Init}");
        byz.PrintList.Add("0 " + byz.Index.ToString() + " " + byz.Init.ToString());
        string parent = "*";
        //Print EIG
        for (int i = 1; i <= byz.MaxLevel; i++) {
            string m = i.ToString()+" "+byz.Index.ToString()+ " ";

            foreach (var key in byz.EIG.Keys) {
                if (key.Length == i && !key.Equals("λ")) {
                    //第一个节点，初始化parent
                    if (parent.Equals("*")) {
                        parent = key.Substring(0, key.Length - 1);
                        //m = m + byz.EIG[key];
                    }
                    //同父节点
                    if (key.Substring(0, key.Length - 1).Equals(parent)) {
                        m = m + byz.EIG[key];
                    }
                    //异父节点
                    if (!key.Substring(0, key.Length - 1).Equals(parent))
                    {
                        m = m + " "+byz.EIG[key];
                        parent = key.Substring(0, key.Length - 1);
                    }
                }
            }

            Console.WriteLine(m);
            byz.PrintList.Add(m);
            parent = "*";
        }

        //Print EIG_eva
        for (int i = byz.MaxLevel;i >= 1; i--) {
            string m = (byz.MaxLevel*2+1-i).ToString()+" "+byz.Index.ToString()+" ";

            foreach (var key in byz.EIG_eva.Keys)
            {
                if (key.Length == i && !key.Equals("λ"))
                {
                    //第一个节点，初始化parent
                    if (parent.Equals("*"))
                    {
                        parent = key.Substring(0, key.Length - 1);
                        //m = m + byz.EIG[key];
                    }
                    //同父节点
                    if (key.Substring(0, key.Length - 1).Equals(parent))
                    {
                        m = m + byz.EIG_eva[key];
                    }
                    //异父节点
                    if (!key.Substring(0, key.Length - 1).Equals(parent))
                    {
                        m = m + " " + byz.EIG_eva[key];
                        parent = key.Substring(0, key.Length - 1);
                    }
                }
            }

            Console.WriteLine(m);
            byz.PrintList.Add(m);
            parent = "*";
        }

        Console.WriteLine((byz.MaxLevel * 2 + 1).ToString() + " " + byz.Index.ToString() + " " + byz.EIG_eva[""]);
        byz.PrintList.Add((byz.MaxLevel * 2 + 1).ToString() + " " + byz.Index.ToString() + " " + byz.EIG_eva[""]);
    }

    public void Print(List<string> str) {
        Console.WriteLine("Shouldn't happen.");
    }

    public void SendToArc() {
        WebChannelFactory<INodeService> wcf = null;
        OperationContextScope scope = null;
        try
        {
            var uri = new Uri($"http://localhost:8090/");
            wcf = new WebChannelFactory<INodeService>(uri);
            var channel = wcf.CreateChannel();

            scope = new OperationContextScope((IContextChannel)channel);
            
            channel.Print(byz.PrintList);

        }
        catch (Exception ex)
        {
            var exmsg = ($"*** Exception {ex.Message}");
            Console.Error.WriteLine(exmsg);
            Console.WriteLine(exmsg);
            wcf = null;
            scope = null;

        }
        finally
        {
            if (wcf != null) ((IDisposable)wcf).Dispose();
            if (scope != null) ((IDisposable)scope).Dispose();
        }
    }

}

public class byz
{
    //rem                          N  L  ID V  V0 F  resp
    //start "byz1" cmd /k byz.exe  4  2  1  0  0  1  byz1.txt

    public static int MaxIndex;
    public static int MaxLevel;
    public static int Index;
    public static int Init;
    public static int V0;
    public static int IsFaulty;
    public static string FileName = "None";
    public static Dictionary<string, int> EIG = new Dictionary<string, int>();
    public static Dictionary<string, int> EIG_eva = new Dictionary<string, int>();
    public static List<string> PrintList = new List<string>();

    private static void Main()
    {
        ReadParamater();
        createEIG();
        BuildHost();
        Console.WriteLine("The final result is");
        foreach (var key in byz.EIG_eva.Keys) {
            Console.WriteLine($"*{key}--> {EIG[key]}");
        }
        
        /*
        Message m1 = new Message(1, 1, 1, "0");
        Message m2 = new Message(1, 2, 1, "0");
        Message m3 = new Message(1, 2, 1, "1");
        Message m4 = new Message(1, 2, 1, "1");


        Message[] input = { m1, m2, m3, m4 };
        var output = Messages(input);
        Console.WriteLine("MMMMM");
        */
    }

    private static void ReadParamater()
    {
        
        string[] commandLineArgs = Environment.GetCommandLineArgs();
        
        if (commandLineArgs.Length >= 2){
            MaxIndex = int.Parse(commandLineArgs[1]);
        }
        if (commandLineArgs.Length >= 3){
            MaxLevel = int.Parse(commandLineArgs[2]);
        }
        if (commandLineArgs.Length >= 4){
            Index = int.Parse(commandLineArgs[3]);
        }
        if (commandLineArgs.Length >= 5){
            Init = int.Parse(commandLineArgs[4]);
        }
        if (commandLineArgs.Length >= 6){
            V0 = int.Parse(commandLineArgs[5]);
        }
        if (commandLineArgs.Length >= 7){
            IsFaulty = int.Parse(commandLineArgs[6]);
        }
        if (commandLineArgs.Length >= 8){
            FileName = commandLineArgs[7];
        }
        
        /*
        MaxIndex = 4;
        MaxLevel = 4;
        Index = 1;
        Init = 0;
        V0 = 0;
        IsFaulty = 0;
        EIG.Add("λ", Index);
        */
    }

    private static void BuildHost()
    {
        WebServiceHost host = null;

        try
        {
            var baseAddress = new Uri($"http://localhost:{Index + 8080}/");
            host = new WebServiceHost(typeof(ByzService), baseAddress);
            ServiceEndpoint ep = host.AddServiceEndpoint(typeof(INodeService), new WebHttpBinding(), "");

            host.Open();

            
            var msg = ($"Byz={Index}: {baseAddress}Message?from=?,to=?,msg=?");
            Console.WriteLine(msg);
            

            Console.ReadLine();
            host.Close();

        }
        catch (Exception ex)
        {
            var msg = ($"*** Exception {ex.Message}");
            Console.Error.WriteLine(msg);
            Console.WriteLine(msg);
            host = null;
        }
        finally
        {
            if (host != null) ((IDisposable)host).Dispose();
        }
    }

    private static void createEIG()
    {
        AppendString("");
        //Console.WriteLine($"EIG size: {EIG.Count}");
    }

    private static void AppendString(string str)
    {
        for (int i = 1; i <= MaxIndex; i++)
        {
            if (!str.Contains(i.ToString()))
            {
                string s = str + i;
                //Console.WriteLine(s);
                EIG.Add(s, -1);
                EIG_eva.Add(s, -1);
                if (s.Length < MaxLevel)
                {
                    AppendString(s);
                }
            }
        }
    }


}