
function delay(secs) {
    return new Promise<number>((resolve, reject) => {
        setTimeout(() => {
            print("[async] resolve");
            resolve(123);
        }, secs * 1000);
    });
}

async function test() {
    print("[async] begin");
    await delay(3);
    print("[async] end");
    let result = <System.Net.IPHostEntry>await jsb.Yield(jsb.AsyncTaskTest.GetHostEntryAsync("www.baidu.com"));
    console.log("host entry:", result.HostName);
}

async function testUnityYieldInstructions() {
    console.warn("wait for unity YieldInstruction, begin");
    await jsb.Yield(new UnityEngine.WaitForSeconds(3));

    console.warn("wait for unity YieldInstruction, end;", UnityEngine.Time.frameCount);
    await jsb.Yield(null);
    console.warn("wait for unity YieldInstruction, next frame;", UnityEngine.Time.frameCount);
}

export function run() {
    test();
    testUnityYieldInstructions();
}