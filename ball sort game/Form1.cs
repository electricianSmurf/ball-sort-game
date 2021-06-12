using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using Newtonsoft.Json;

namespace ball_sort_game
{
    public partial class Form1 : Form
    {
        cBottleObj bottleProperties;
        cBallObj ballProperties;
        List<cBottleObj> listBottleObjects;

        int numberOfBottlesToBeProduced;
        int round;
        int filledBottleCount = 0;
        int width = 600;
        int targetLocY;
        int ballTag = 0;
        int clickCounter = 0;
        bool firstClick, secondClick;
        bool secondMoveAllowed = false;
        bool isRoundNew;
        PictureBox movingBall;
        PictureBox targetBottle;
        Random rnd = new Random();
        List<Image> colors_lst = new List<Image>();
        List<PictureBox> bottle;
        List<List<PictureBox>> list2dBallsInBottles = new List<List<PictureBox>>();
        List<PictureBox> listBottles = new List<PictureBox>();
        List<int> listColoursToBeProducedAtLevel = new List<int>();
        List<int> listBallColoursAtLevel = new List<int>();
        List<PictureBox> allPBoxes = new List<PictureBox>();// this list is created for deleting all of bottles and
                                                            // balls easily
        public Form1()
        {
            InitializeComponent();
            fillColourList();
        }

        void fillColourList()
        {
            //15 colours
            colors_lst.Add(Properties.Resources.red);// 0
            colors_lst.Add(Properties.Resources.black);// 1
            colors_lst.Add(Properties.Resources.yellow);// 2
            colors_lst.Add(Properties.Resources.brown);// 3
            colors_lst.Add(Properties.Resources.purple);// 4
            colors_lst.Add(Properties.Resources.white);// 5
            colors_lst.Add(Properties.Resources.green);// 6
            colors_lst.Add(Properties.Resources.blue);// 7
            colors_lst.Add(Properties.Resources.pink);// 8
            colors_lst.Add(Properties.Resources.orange);// 9
            colors_lst.Add(Properties.Resources.maroon);// 10
            colors_lst.Add(Properties.Resources.silver);// 11
            colors_lst.Add(Properties.Resources.gold);// 12
            colors_lst.Add(Properties.Resources.gray);// 13
            colors_lst.Add(Properties.Resources.cyan);// 14
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            getSettings();
            if (!isRoundNew)
            {
                startGame();
            }
        }

        private void getSettings()
        {
            numberOfBottlesToBeProduced = Properties.Settings.Default.level;
            round = Properties.Settings.Default.cycle;
            isRoundNew = Properties.Settings.Default.isRoundNew;
            
            btnStart.Visible = false;
            if (isRoundNew)
            {
                btnStart.Visible = true;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Visible = false;

            startGame();

            Properties.Settings.Default.isRoundNew = false;
            Properties.Settings.Default.Save();
        }

        private void startGame()
        {
            for (int i = 0; i < numberOfBottlesToBeProduced; i++)
            {
                produceBottle(i);
            }
            
            round++;

            if (isRoundNew)
            {
                selectRandomColors();
                alignBallColoursAtLevel();

                //now mix our list completely randomly
                listBallColoursAtLevel = listBallColoursAtLevel.OrderBy(a => Guid.NewGuid()).ToList();

                placeProducedBallsIntoBottles();

            }
            else
            {
                string path = validateFolderPath() + @"\bottles.json";
                StreamReader reader = new StreamReader(path);
                string jsonData = reader.ReadToEnd();
                List<cBottleObj> bottleObjsJson = JsonConvert.DeserializeObject<List<cBottleObj>>(jsonData);
                
                for (int bottleNumber = 0; bottleNumber < bottleObjsJson.Count; bottleNumber++)
                {
                    for (int ballNumber = 0; ballNumber < bottleObjsJson[bottleNumber].listBottleObjBalls.Count; ballNumber++)
                    {
                        PictureBox PBox = new PictureBox();
                        PBox.BackColor = Color.Transparent;
                        PBox.Image = colors_lst[bottleObjsJson[bottleNumber].listBottleObjBalls[ballNumber].ballObjColour];
                        PBox.SizeMode = PictureBoxSizeMode.StretchImage;
                        PBox.Click += pb_Click;
                        PBox.Size = new Size(22, 22);
                        PBox.Name = "ball";
                        
                        PBox.Location = bottleObjsJson[bottleNumber].listBottleObjBalls[ballNumber].ballObjLoc;
                        Controls.Add(PBox);

                        list2dBallsInBottles[bottleNumber].Add(PBox);
                        allPBoxes.Add(PBox);
                        PBox.BringToFront();
                    }
                }
            }
            
            levelInfo.Text = "Level: " + numberOfBottlesToBeProduced.ToString() + " / 16, Round: " + round.ToString() + " / " + numberOfBottlesToBeProduced.ToString();
            
            ball_mover.Start();
        }

        private void saveSettings()
        {
            Properties.Settings.Default.level = numberOfBottlesToBeProduced;
            if (numberOfBottlesToBeProduced > 16)
            {
                Properties.Settings.Default.level = 3;
            }
            Properties.Settings.Default.cycle = round;
            
            Properties.Settings.Default.isRoundNew = isRoundNew;
            Properties.Settings.Default.Save();
        }
        void produceBottle(int bottleNumber)
        {
            PictureBox pb = new PictureBox();
            pb = createPBoxObject(pb, true, bottleNumber);
            //bottleNumber is that which bottle is being produced

            int gapBetweenBottles = 0;
            int bottleLocY = calculatingBottleLocY(bottleNumber);
            int bottleLocX = 0;

            if (numberOfBottlesToBeProduced % 2 == 0)//producing even numbered bottles
            {
                gapBetweenBottles = calculateGap(true);
                if (bottleLocY == 300)
                {
                    //300 means that bottles are now below.
                    //e.g: if numberOfBottles = 6 --> (0,1,2 up) then 3rd bottle is below. 3 - (6 / 2) = 0
                    //bottleLocX = gap * (0 + 1)
                    bottleNumber -= numberOfBottlesToBeProduced / 2;
                }
                //bottleNumber starts counting from 0, this is why 1 is added
                bottleLocX = gapBetweenBottles * (bottleNumber + 1);
                pb.Location = new Point(bottleLocX, bottleLocY);
            }
            else//producing odd numbered bottles
            {
                gapBetweenBottles = calculateGap(false);
                if (bottleLocY == 120)
                {
                    bottleLocX = gapBetweenBottles * (bottleNumber + 1);
                    pb.Location = new Point(bottleLocX, bottleLocY);
                }
                else
                {
                    bottleNumber -= (numberOfBottlesToBeProduced + 1) / 2;
                    bottleLocX = (gapBetweenBottles / 2) + gapBetweenBottles * (bottleNumber + 1);
                    pb.Location = new Point(bottleLocX, bottleLocY);
                }
            }

            bottle = new List<PictureBox>();
            list2dBallsInBottles.Add(bottle);
            allPBoxes.Add(pb);
            listBottles.Add(pb);
            Controls.Add(pb);
        }
        private PictureBox createPBoxObject(PictureBox PBox, bool isBottleOrBall, int tag)
        {
            PBox.BackColor = Color.Transparent;
            PBox.SizeMode = PictureBoxSizeMode.StretchImage;
            PBox.Tag = tag;
            PBox.Click += pb_Click;

            if (isBottleOrBall)
            {
                PBox.Image = Properties.Resources.bottle;
                PBox.Name = "bottle";
                PBox.Size = new Size(40, 110);
            }
            else
            {
                PBox.Name = "ball";
                PBox.Size = new Size(22, 22);
                ballTag++;
            }
            return PBox;
        }
        private int calculateGap(bool isEvenOrOdd)
        {
            int gap = 0;
            if (isEvenOrOdd)//bottle number is even
            {
                gap = width / ((numberOfBottlesToBeProduced / 2) + 1);
            }
            else//bottle number is odd
            {
                gap = width / (((numberOfBottlesToBeProduced + 1) / 2) + 1);
            }
            return gap;
        }
        private int calculatingBottleLocY(int bottleNumber)
        {
            int locY = 0;
            //(numberOfBottlesToBeProduced + 1) --> adding 1 is for easing the operation of getting down line
            //e.g: numberOfBottles = 8 --> (8 + 1) / 2 = 4,5. 4 bottles are up, 4 bottles are below
            //if it's 9, then (9 + 1) / 2 = 5. 5 bottles are up, 4 bottles are below
            if (bottleNumber < (numberOfBottlesToBeProduced + 1) / 2)
            {
                locY = 120;
            }
            else
            {
                locY = 300;
            }
            return locY;
        }
        void selectRandomColors()
        {
            for (int i = 0; i < numberOfBottlesToBeProduced - 1; i++)
            {
                int nmbr = rnd.Next(0, colors_lst.Count);
                if (!listColoursToBeProducedAtLevel.Contains(nmbr))
                {
                    listColoursToBeProducedAtLevel.Add(nmbr);
                }
                else
                {
                    i--;
                }
            }
        }
        void alignBallColoursAtLevel()
        {
            for (int i = 0; i < listColoursToBeProducedAtLevel.Count; i++)
            {
                for (int k = 0; k < 4; k++)
                {
                    listBallColoursAtLevel.Add(listColoursToBeProducedAtLevel[i]);
                }
            }
        }
        void placeProducedBallsIntoBottles()
        {
            int counter = 0;
            for (int bottleNumber = 0; bottleNumber < listColoursToBeProducedAtLevel.Count; bottleNumber++)
            {
                for (int ballNumber = 0; ballNumber < 4; ballNumber++)
                {
                    int ballColour = listBallColoursAtLevel[counter];
                    counter++;
                    list2dBallsInBottles[bottleNumber].Add(produceBall(ballNumber, ballColour, bottleNumber));
                }
            }
        }
        public PictureBox produceBall(int ballNumber, int colorNumber, int bottleNumber)
        {
            PictureBox pb = new PictureBox();
            createPBoxObject(pb, false, ballTag);
            pb.Image = colors_lst[colorNumber];
            int locX = listBottles[bottleNumber].Location.X + 9;
            int locY = listBottles[bottleNumber].Location.Y + 74 - (pb.Height * ballNumber);
            pb.Location = new Point(locX, locY);
            Controls.Add(pb);
            allPBoxes.Add(pb);
            pb.BringToFront();
            return pb;
        }
        void pb_Click(object sender, EventArgs e)
        {
            PictureBox clickedPb = sender as PictureBox;
            clickCounter++;
            detectClickTurn();

            //ball_mover has been working since the start button is clicked, so when the target locY changes
            //ball_mover executes the movement
            if (firstClick)
            {
                //if player clickes a ball, find the bottle which the clicked ball is in
                if (clickedPb.Name == "ball")
                {
                    operateBallClicked1st(clickedPb);
                }
                //if player clickes a bottle
                else
                {
                    operateBottleClicked1st(clickedPb);
                }
            }

            else if (secondClick)
            {
                // if player clickes a ball, find bottle which the clicked ball is in
                if (clickedPb.Name == "ball")
                {
                    operateBallClicked2nd(clickedPb);
                }
                //if player clickes a bottle pic
                else
                {
                    operateBottleClicked2nd(clickedPb);
                }
            }
        }
        void detectClickTurn()
        {
            if (clickCounter == 1 && !secondClick)
            {
                firstClick = true;
            }
            else if (clickCounter == 2 && !firstClick)
            {
                secondClick = true;
            }
        }
        void operateBallClicked1st(PictureBox clickedBall)
        {
            foreach (List<PictureBox> bottle in list2dBallsInBottles)
            {
                if (bottle.Contains(clickedBall))
                {
                    //remove ball from the bottle and move it over its bottle
                    movingBall = bottle.Last();
                    targetLocY = listBottles[list2dBallsInBottles.IndexOf(bottle)].Location.Y - 30;
                    bottle.Remove(bottle.Last());
                }
            }
        }
        void operateBottleClicked1st(PictureBox clickedBottle)
        {
            int bottleTag = Convert.ToInt32(clickedBottle.Tag);
            if (list2dBallsInBottles[bottleTag].Count == 0)//if player clicks an empty bottle
            {
                clickCounter--;
                firstClick = false;
            }
            else
            {
                movingBall = list2dBallsInBottles[bottleTag].Last();//the ball at the top of the bottle
                targetLocY = listBottles[bottleTag].Location.Y - 30;//move the ball over its bottle
                list2dBallsInBottles[bottleTag].Remove(list2dBallsInBottles[bottleTag].Last());
            }
        }
        void operateBallClicked2nd(PictureBox clickedBall)
        {
            foreach (List<PictureBox> bottle in list2dBallsInBottles)
            {
                if (bottle.Contains(clickedBall))
                {
                    if(bottle.Count == 4)//if bottle is full(involves 4 balls)
                    {
                        clickCounter--;
                        secondClick = false;
                        break;
                    }
                    else
                    {
                        targetBottle = listBottles[list2dBallsInBottles.IndexOf(bottle)];
                        clickCounter = 0;
                        secondMoveAllowed = true;
                        break;
                    }
                }
            }
        }
        void operateBottleClicked2nd(PictureBox clickedBottle)
        {
            if (list2dBallsInBottles[Convert.ToInt32(clickedBottle.Tag)].Count == 4)
            {
                clickCounter--;
            }
            else
            {
                targetBottle = clickedBottle;
                clickCounter = 0;
                secondMoveAllowed = true;
            }
        }

        void moveBall()
        {
            if (firstClick)
            {
                movingBall.Location = new Point(movingBall.Location.X, targetLocY);
                firstClick = false;
            }
            else if (secondClick && secondMoveAllowed)
            {
                moveBallToTarget();
                putBallIntoBottle();

                secondClick = false;
                secondMoveAllowed = false;
                checkAreBottlesFull();
            }
        }
        void moveBallToTarget()
        {
            Point velocity = calculateBallVelocity();
            //i < 3 is for completing the movement in 3 turns as if the ball is moving
            for (int i = 0; i < 3; i++)
            {
                movingBall.Location = new Point(movingBall.Location.X + velocity.X, movingBall.Location.Y + velocity.Y);
            }
        }
        private Point calculateBallVelocity()
        {
            int ballVelocityX = (targetBottle.Location.X - movingBall.Location.X) / 3;
            int ballVelocityY = (targetBottle.Location.Y - movingBall.Location.Y) / 3;
            Point p = new Point(ballVelocityX, ballVelocityY);
            return p;
        }
        void putBallIntoBottle()
        {
            int bottleNumber = Convert.ToInt32(targetBottle.Tag);
            int ballNumberInBottle = list2dBallsInBottles[bottleNumber].Count;
            //balLocX --> +9 is for putting the ball in the middle of bottle
            int balLocX = listBottles[bottleNumber].Location.X + 9;
            //balLocY --> 300 is top of the bottle.(300 + 74) is bottom of bottle.
            int balLocY = listBottles[bottleNumber].Location.Y + 74 - movingBall.Height * ballNumberInBottle;
            movingBall.Location = new Point(balLocX, balLocY);
            list2dBallsInBottles[bottleNumber].Add(movingBall);
        }
        void checkAreBottlesFull()
        {
            filledBottleCount = 0;
            for (int bottleNumber = 0; bottleNumber < list2dBallsInBottles.Count; bottleNumber++)
            {
                if (isBottleBallsTheSame(bottleNumber) && list2dBallsInBottles[bottleNumber].Count == 4)
                {
                    filledBottleCount++;
                }
            }
            if (filledBottleCount == list2dBallsInBottles.Count - 1)//one of the bottles is empty at the end of the game
            {
                ball_mover.Stop();
                reStart();
            }
        }
        private bool isBottleBallsTheSame(int bottleNumber)
        {
            bool same = false;
            if (list2dBallsInBottles[bottleNumber].All(item => item.Image == list2dBallsInBottles[bottleNumber].First().Image))
            {
                same = true;
            }
            return same;
        }
        void reStart()
        {
            removeAllPBoxes();
            clearAllLists();
            emptyAllVariables();
            checkRound();
            checkLevel();

            isRoundNew = true;
            saveSettings();
            btnStart.Visible = true;
        }
        void removeAllPBoxes()
        {
            for (int i = 0; i < allPBoxes.Count; i++)
            {
                Controls.Remove(allPBoxes[i]);
            }
        }
        void clearAllLists()
        {
            allPBoxes.Clear();
            allPBoxes.TrimExcess();

            list2dBallsInBottles.Clear();
            list2dBallsInBottles.TrimExcess();

            listBottles.Clear();
            listBottles.TrimExcess();

            listColoursToBeProducedAtLevel.Clear();
            listColoursToBeProducedAtLevel.TrimExcess();

            listBallColoursAtLevel.Clear();
            listBallColoursAtLevel.TrimExcess();
        }
        void emptyAllVariables()
        {
            ballTag = 0;
            clickCounter = 0;
            firstClick = false;
            secondClick = false;
            secondMoveAllowed = false;
            movingBall = null;
            targetBottle = null;
        }
        void checkRound()
        {
            if (round == numberOfBottlesToBeProduced)
            {
                round = 0;
                numberOfBottlesToBeProduced++;
            }
        }
        void checkLevel()
        {
            if (numberOfBottlesToBeProduced > 16)
            {
                numberOfBottlesToBeProduced = 3;
            }
        }
        private void ball_mover_Tick(object sender, EventArgs e)
        {
            moveBall();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (!btnStart.Visible)
            {
                createBottleObj();
                createJsonObject();
                this.Close();
            }
            else
            {
                MessageBox.Show("Please press start first!");
            }
        }

        void createBottleObj()
        {
            listBottleObjects = new List<cBottleObj>();
            for (int bottleNumb = 0; bottleNumb < numberOfBottlesToBeProduced; bottleNumb++)
            {
                bottleProperties = new cBottleObj();

                bottleProperties.listBottleObjBalls = new List<cBallObj>();
                
                if (list2dBallsInBottles[bottleNumb].Count != 0)
                {
                    for (int ballNumb = 0; ballNumb < list2dBallsInBottles[bottleNumb].Count; ballNumb++)
                    {
                        bottleProperties.listBottleObjBalls.Add(createBallObj(bottleNumb, ballNumb));
                    }
                }
                
                listBottleObjects.Add(bottleProperties);
            }
        }

        cBallObj createBallObj(int bottleNumb, int ballNumb)
        {
            ballProperties = new cBallObj();
            
            ballProperties.ballObjLoc = list2dBallsInBottles[bottleNumb][ballNumb].Location;
            ballProperties.ballObjColour = colors_lst.IndexOf(list2dBallsInBottles[bottleNumb][ballNumb].Image);

            return ballProperties;
        }
        
       void createJsonObject()
        {
            string jsonResult = JsonConvert.SerializeObject(listBottleObjects);
            string path = validateFolderPath() + @"\bottles.json";
            
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (var write = new StreamWriter(path, true))
            {
                write.WriteLine(jsonResult.ToString());
                write.Close();
            }
        }

        private string validateFolderPath()
        {
            string folderPath = AppDomain.CurrentDomain.BaseDirectory + @"\properties\";

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    DirectoryInfo directory = Directory.CreateDirectory(folderPath);
                }
            }
            catch (Exception){}

            return folderPath;
        }

        private void btnMinimise_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
    }
}
