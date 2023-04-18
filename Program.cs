﻿using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using ScreenRecorderLib;

var profilepath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"arteaststudio.txt");
string? email = "";
string? password = "";

if(File.Exists(profilepath)){
    var info = File.ReadAllLines(profilepath);
    Console.WriteLine("Loaded "+email+" from "+profilepath);
    email = info[0];
    password = info[1];
}else{
    Console.Write("Enter Email: ");
    email = Console.ReadLine();
    Console.Write("Enter Password: ");
    password = Console.ReadLine();
    if(email != null && password != null){
        File.WriteAllLines(profilepath,new string[]{email,password});
        Console.WriteLine("Info saved to "+profilepath);
    }else{
        throw new Exception("Invalid Email/Password");
    }
}

var chromeOptions = new ChromeOptions();
chromeOptions.AddExcludedArgument("enable-automation");
chromeOptions.AddAdditionalChromeOption("useAutomationExtension", false);
chromeOptions.AddUserProfilePreference("credentials_enable_service",false);
chromeOptions.AddUserProfilePreference("profile.password_manager_enabled",false);
chromeOptions.AddArgument("--password-store=basic");

new DriverManager().SetUpDriver(new ChromeConfig());
using (var driver = new ChromeDriver(chromeOptions)){
    try{
        driver.Manage().Window.Maximize();
        driver.Navigate().GoToUrl("https://members.arteaststudio.net");
        driver.FindElement(By.LinkText("我的课程")).Click();

        new WebDriverWait(driver, TimeSpan.FromSeconds(30))
            .Until(drv => drv.FindElement(By.Id("CustomerEmail")))
            .SendKeys(email);
        new WebDriverWait(driver, TimeSpan.FromSeconds(5))
            .Until(drv => drv.FindElement(By.Id("CustomerPassword")))
            .SendKeys(password);
        new WebDriverWait(driver, TimeSpan.FromSeconds(5))
            .Until(drv => drv.FindElement(By.Id("customer_login")))
            .Submit();
        new WebDriverWait(driver, TimeSpan.FromSeconds(30))
            .Until(drv => drv.FindElement(By.CssSelector("button[data-prod-title='华艺古典舞会员']")))
            .Click();
        new WebDriverWait(driver, TimeSpan.FromSeconds(30))
            .Until(drv => drv.FindElement(By.CssSelector("h2[id='courseTitle']")).Text == "华艺古典舞会员");

        Thread.Sleep(500);

        while(driver.FindElements(By.CssSelector("span.nestedMain[data-attr-view='0']")).Count > 0){
            driver.ExecuteScript("document.querySelectorAll(\"span.nestedMain[data-attr-view='0']\")[0].click()");
        }

        var getLessons = (IWebDriver driver)
            => driver.FindElements(By.CssSelector("li.list-group-item[data-attr-id]"));

        var lessonCount = getLessons(driver).Count;

        Console.WriteLine("Listing Lessons");
        for(var i = 0;i<lessonCount;i++){
            var lessonName = getLessons(driver)[i]
                .FindElement(By.CssSelector("span.cLessonTitle"))
                .GetAttribute("textContent");
            var expectedFile = lessonName+".mp4";
            if(File.Exists(expectedFile)){
                Console.WriteLine(expectedFile+" already exists");
                continue;
            }
            Console.WriteLine("Playing "+lessonName);
            driver.ExecuteScript($"document.querySelectorAll(\"li.list-group-item[data-attr-id]\")[{i}].click()");
            Thread.Sleep(500);
            try{
                var vid = new WebDriverWait(driver, TimeSpan.FromSeconds(1))
                    .Until(drv=>
                        drv.FindElement(By.CssSelector("div.videoWrapper>iframe")));
                Console.WriteLine(vid.GetAttribute("src"));
                driver.ExecuteScript("document.querySelector('div.videoWrapper').scrollIntoView()");
                var inner = driver.SwitchTo().Frame(driver.FindElement(By.CssSelector("div.videoWrapper>iframe")));
                if(inner != null && inner is ChromeDriver innerchrome)
                {
                    innerchrome.FindElement(By.CssSelector("button[aria-label=\"Play\"]")).Click();
                    innerchrome.FindElement(By.CssSelector("button[aria-label=\"Enter full screen\"]")).Click();
                    Recorder rec = Recorder.CreateRecorder();
                    rec.Record(expectedFile);
                    Thread.Sleep(10000);//wait 10 seconds to ensure playback progresses beyond that
                    rec.Stop();
                    innerchrome.ExecuteScript("document.querySelector('button[aria-label=\"Exit full screen\"]').click()");
                    //innerchrome.FindElement(By.CssSelector("button[aria-label=\"Exit full screen\"]")).Click();
                    
                    inner.SwitchTo().ParentFrame();
                }
                else{
                    throw new Exception("inner is not valid");
                }
            }catch(WebDriverTimeoutException){
                Console.WriteLine("No video");
            }
        }

        Console.WriteLine("Done");
    }finally{
        driver.Close();
    }
}

