/**************************************************************************
* Copyright (C) echoAR, Inc. 2018-2020.                                   *
* echoAR, Inc. proprietary and confidential.                              *
*                                                                         *
* Use subject to the terms of the Terms of Service available at           *
* https://www.echoar.xyz/terms, or another agreement                      *
* between echoAR, Inc. and you, your company or other organization.       *
***************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomBehaviour : MonoBehaviour
{
    [HideInInspector]
    public Entry entry;

    /// <summary>
    /// EXAMPLE BEHAVIOUR
    /// Queries the database and names the object based on the result.
    /// </summary>

    //TODO: Game over/restart screen

    //create a 2d array for game state 1:red, 2:black
    static int[,] gameState = {{0,1,0,0,0,2,0,2},
                               {1,0,1,0,0,0,2,0},
                               {0,1,0,0,0,2,0,2},
                               {1,0,1,0,0,0,2,0},
                               {0,1,0,0,0,2,0,2},
                               {1,0,1,0,0,0,2,0},
                               {0,1,0,0,0,2,0,2},
                               {1,0,1,0,0,0,2,0}};
    //stores the distance between sqares on the 3d model game board
    static float dist_btw = 5.5F;
    //store a reference to the board for attaching new game objects
    public static GameObject board;

    //stores the game board as an array of game objects to 
    static GameObject[,] objGameState = new GameObject[8,8];
    //list of possible movs
    static List<GameObject> pos_moves = new List<GameObject>();
    //list of king objects
    static List<GameObject> kings = new List<GameObject>();
    //the selected piece
    static GameObject selected;


    //left-most bound of the game board
    float left_bound = -19F;
    //bottom-most bound of the game board
    float bot_bound = -19.5F;
    //stores the moves in "x,y" and which pieces are taken
    static Dictionary<string,List<int[]>> take_moves_dict = new Dictionary<string,List<int[]>>();
    //store who's move it is
    static bool isRedMove = true;

    //takes the x value on the game board
    //@returns the x value on the 3d model game board
    float getBoardX(int x){
        //[0,8) -> [-19,19.5]
        return ((float)(x)*dist_btw) +left_bound;
    }
    //takes the y value on the game board
    //@returns the y value on the 3d model game board
    float getBoardY(int y){
        //[0,8) -> [-19.5,19]
        return ((float)(y)*dist_btw) + bot_bound;
    }
    //create method(s) for game location to game state index
    //distance between squares is .5
    //param: accepts a gameObject's position
    //returns x value for gameState index
    int getX(Vector3 pos){
        //[-19, 19.5] -> [0,8)
        float v= ((pos[0]-left_bound)/(dist_btw));
        return (int) System.Math.Round(v);

    }

    //gets the y index on the board of a piece
    int getY(Vector3 pos){
        //[-19.5,19] -> [0,8)
        float v = ((pos[2]-bot_bound)/(dist_btw));
        return (int)System.Math.Round(v);
    }

    //checks if (x,y) is in the bounds of the game board
    bool inBounds(int x,int y){
        if(x<0 | x>7 | y<0 | y>7){
            return false;
        }
        return true;
    }

    //checks if the space at x,y is empty
    bool isEmptySpace(int x, int y){
        try{
            return gameState[x,y] == 0;
        }catch{
            Debug.Log(x);
            Debug.Log(y);
            return false;
        }
    }

    //checks if a piece can be moved to x,y
    bool allowableMove(int x, int y){
        if(inBounds(x,y)){
           return isEmptySpace(x,y);
        }
        return false;
    }
    //checks if there is an opposite piece in the location (x,y)
    bool oppositePieceIn(int x, int y, int r_or_b){
        if(inBounds(x,y)){
            return gameState[x,y] !=0 & gameState[x,y] != r_or_b;
        }
        return false;
    }

    //Gets possible taking moves
    //@param x the x location on the game board
    //@param y the y location on the game board
    //@param r_or_b 1 or 2 for if the piece is red or black
    //@param isKing if the piece is a king or not
    //@param taken_pieces recursively store the taken pieces
    //@return a list of int[x,y] of possible moves that take a piece
    List<int[]> getTakeMoves(int x, int y, int r_or_b, bool isKing,List<int[]> taken_pieces){
        //list of possible x values where you can take
        List<int> take_x = new List<int>();
        //add jumping to the left and to the right
        take_x.Add(x+2);
        take_x.Add(x-2);
        //list of possible y values where you can take
        List<int> take_y = new List<int>();

        //moving forward or backward depending on if the piece is red,black or a king
        if(r_or_b == 1 | isKing){//red moves forward
            take_y.Add(y+2);
        }
        if(r_or_b ==2 |isKing){//black moves backwards
            take_y.Add(y-2);
        }
        //output list of possible take moves
        List<int[]> take_moves = new List<int[]>();
        //iterating over all possible moves
        foreach (int x_move in take_x){
            foreach (int y_move in take_y){
                //use the distance moved to get where the taken piece is
                int x_delta = x_move-x;
                int y_delta = y_move-y;
                //the move needs to be allowable in the game board and there needs to be an opposite piece
                //in the square between to be able to take the piece
                if(allowableMove(x_move,y_move) & oppositePieceIn(x+x_delta/2,y+y_delta/2,r_or_b)){
                    //append to move list
                    int[] move = {x_move,y_move};
                    //location of the taken piece
                    int [] taken_piece = {(x+x_delta/2),(y+y_delta/2)};
                    //add to the returned list
                    taken_pieces.Add(taken_piece);

                    //the key for our dictionary of taking moves
                    string key = x_move.ToString()+","+y_move.ToString();
                    //add list of taken pieces for that move to the dictionary
                    take_moves_dict[key]=new List<int[]>(taken_pieces);

                    //recursively check for multi-jumps from new poition
                    List<int[]> multi_jumps = getTakeMoves(x_move,y_move,r_or_b,isKing,taken_pieces);
                    //remove the taken piece from our list for checking the next possible taking move
                    taken_pieces.Remove(taken_piece);
                    //add all possible multi jumps to the possible moves for the piece
                    take_moves.AddRange(multi_jumps);
                    //add the current move to the possible moves for the piece
                    take_moves.Add(move);
                } 
            }
        }
        return take_moves;
    }
    //check for available moves
    //@param x x location of the piece
    //@param y y locaiton of the piece
    //@param r_or_b 1 for red 2 for black
    //@param isKing if the piece is a king or not
    List<int[]> checkMoves(int x, int y, int r_or_b, bool isKing){
        //check normal moves
        //list of possible x values to move to
        List<int> x_moves= new List<int>();
        x_moves.Add(x+1);
        x_moves.Add(x-1);
        //list of possible y values to move to
        List<int> y_moves= new List<int>();

        //adding possible y moves based on type and if the piece is a king
        if(r_or_b == 1 | isKing ){//red moves forward
            y_moves.Add(y+1);
        }
        if(r_or_b ==2 | isKing){//black moves backwards
            y_moves.Add(y-1);
        }

        //list of moves to return
        List<int[]> all_moves = new List<int[]>();


        //iterate through all possible moves
        foreach (int x_move in x_moves){
            foreach (int y_move in y_moves){
                //if the move is legal add it to the list
                if(allowableMove(x_move,y_move)){
                    //append to move list
                    int[] move = {x_move,y_move};
                    all_moves.Add(move);
                } 
            }
        }

        //get the take moves as well
        List<int[]> take_moves = getTakeMoves(x,y,r_or_b,isKing, new List<int[]>());

        //and add the take moves to the list
        all_moves.AddRange(take_moves);
        return all_moves;
    }
    //checks for win condition
    //Unused!
    string winCondition(){
        //check win state:
        int r_count=0;
        int b_count=0;
        foreach(int x in gameState){
            if(x==1){
                r_count+=1;
            }
            if(x==2){
                b_count+=1;
            }

        }
        if(r_count==0 | b_count==0){
            return "Game Over";
        }
        return "Continue";
        //TODO: check for stalemate (current player can't make any moves)

    }
    string board_name = "Game Board (6).glb";
    string red_piece_name = "red_piece.glb";
    string black_piece_name = "b_piece.glb";
    //called before Start, get the board's game object reference to make as a parent
    void Awake(){
        //if the game object being processed is the game board
        if(this.gameObject.name.Equals(board_name)){
            //set the reference to add as a parent to all other objects
            board=this.gameObject;
        }
    }
    
    // Use this for initialization
    void Start()
    {
        // Add RemoteTransformations script to object and set its entry
        if(this.gameObject.name.Equals(board_name)){//restricting transformations to just the game board
            this.gameObject.AddComponent<RemoteTransformations>().entry = entry;
        }else{
            // Query additional data to get the name
            string value = "";
            //this.gameObject.transform.SetParent(board.transform);
            
            //create game objects in each row, alternating to match the pattern of the board
            if(this.gameObject.name.Equals(red_piece_name)){
                this.gameObject.AddComponent<BoxCollider>();
                this.gameObject.name = "r_piece";
                //row C
                for (int i = -2; i < 2; i++)
                {
                    GameObject o = Instantiate(this.gameObject, new Vector3(i * 2*dist_btw +8.5F , 0, -8.5F), Quaternion.identity,board.transform);
                    objGameState[getX(o.transform.localPosition),getY(o.transform.localPosition)] = o;
                }
                //row B
                for (int i = -2; i < 2; i++)
                {
                    GameObject o = Instantiate(this.gameObject, new Vector3(i * 2*dist_btw +3F , 0, -14F), Quaternion.identity,board.transform);
                    objGameState[getX(o.transform.localPosition),getY(o.transform.localPosition)] = o;
                }  
                //row A
                for (int i = -2; i < 2; i++)
                {
                    GameObject o = Instantiate(this.gameObject, new Vector3(i * 2*dist_btw +8.5F , 0, -19.5F), Quaternion.identity,board.transform);
                    objGameState[getX(o.transform.localPosition),getY(o.transform.localPosition)] = o;
                }
                Destroy(this.gameObject); 
            }
            if(this.gameObject.name.Equals(black_piece_name)){
                this.gameObject.AddComponent<BoxCollider>();
                this.gameObject.name = "b_piece";
                //row F
                for (int i = -2; i < 2; i++)
                {
                    GameObject o = Instantiate(this.gameObject, new Vector3(i * 2*dist_btw +3F , 0, 8F), Quaternion.identity,board.transform);
                    objGameState[getX(o.transform.localPosition),getY(o.transform.localPosition)] = o;
                }
                //row G
                for (int i = -2; i < 2; i++)
                {
                    GameObject o = Instantiate(this.gameObject, new Vector3(i * 2*dist_btw +8.5F , 0, 13.5F), Quaternion.identity,board.transform);
                    objGameState[getX(o.transform.localPosition),getY(o.transform.localPosition)] = o;
                }  
                //row H
                for (int i = -2; i < 2; i++)
                {
                    GameObject o = Instantiate(this.gameObject, new Vector3(i * 2*dist_btw +3F , 0, 19F), Quaternion.identity,board.transform);
                    objGameState[getX(o.transform.localPosition),getY(o.transform.localPosition)] = o;
                }
                Destroy(this.gameObject); 
            }
            

            
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        //if the game object being processed is a game piece and it's that player's move
        if((this.gameObject.name.Equals("r_piece(Clone)") & isRedMove)| (this.gameObject.name.Equals("b_piece(Clone)")& !isRedMove)){
            //handle game piece touches
            
            if(Input.touchCount >0){
                //get the touch input and check if the projected ray hits a game piece
                Touch touch = Input.GetTouch(0);
                Vector2 touchPos = touch.position;
                //ray from camera to check if it hit an object
                Ray ray = Camera.main.ScreenPointToRay(touchPos);
                RaycastHit hit;
                if(touch.phase == TouchPhase.Began){
                    if (Physics.Raycast(ray, out hit)){
                        //if the ray hit a game object and it is this object
                        if(hit.collider != null & hit.collider.gameObject == this.gameObject){
                            //handle the touch of the game piece
                            handleTouch(hit.collider.gameObject);
                        }
                    }
                }
            }

        }
        //if the game object being processed is a possible move
        if(this.gameObject.name.Equals("r_piece(Clone)(Clone)") | this.gameObject.name.Equals("b_piece(Clone)(Clone)")){
            //handle the touch of the object
            if(Input.touchCount >0){
                Touch touch = Input.GetTouch(0);
                Vector2 touchPos = touch.position;
                //cast a ray to check if touched an object
                Ray ray = Camera.main.ScreenPointToRay(touchPos);
                RaycastHit hit;
                if (touch.phase == TouchPhase.Began)//on touch of object
                {
                    if (Physics.Raycast(ray, out hit))
                    {
                        if (hit.collider != null & hit.collider.gameObject == this.gameObject)
                        {//this object has been selected
                            //handle moving the piece
                            handleMovePiece(hit.collider.gameObject);
                        }
                    }
                }
            }
        }
        
        

    }

    //handles touch of game piece
    //@param g the touched game piece
    void handleTouch(GameObject g){
        //get the game state x and y value of the selected piece
        int x = getX(g.transform.localPosition);
        int y = getY(g.transform.localPosition);
        //get rid of the take moves from the previously selected piece
        take_moves_dict=new Dictionary<string, List<int[]>>();//reset take posibilities
        //set the selected game object to this object
        selected=this.gameObject;
        //check if the piece is red or black
        int r_or_b=0;
        if(g.name.Equals("r_piece(Clone)")){
            r_or_b=1;
        }else{
            r_or_b=2;
        }
        //check for possible moves
        List<int[]> moves = checkMoves(x,y,r_or_b,kings.Contains(g));
        
        //if there are possible moves, add in game objects at those locations to show possible moves
        if(moves.Count > 0){
            if(pos_moves.Count!=0){
                foreach (GameObject obj in pos_moves){
                    Destroy(obj);
                }
                pos_moves= new List<GameObject>();
            }

            foreach(int[] move in moves){
                //mark possible moves on the board
                GameObject p = Instantiate(this.gameObject,board.transform);
                p.transform.localPosition=new Vector3(getBoardX(move[0]), 0, getBoardY(move[1]));
                //mark this object as half as light so you know it's just a possible move
                Color color = p.GetComponentInChildren<MeshRenderer>().material.color ;
                color.a -= .5F;
                p.GetComponentInChildren<MeshRenderer>().material.color = color ;
                //add box collider for touch capabiliy
                p.AddComponent<BoxCollider>();
                //add the move to the possible moves to be able to delete later
                pos_moves.Add(p);
            }
        }
    }
    //handles clicking on a possible move
    //@param g selected move option piece
    void handleMovePiece(GameObject g){

        //change who's move it is now
        isRedMove = !isRedMove;
        //update gamestate
        int selected_x = getX(selected.transform.localPosition);
        int selected_y = getY(selected.transform.localPosition);
        gameState[selected_x, selected_y] = 0;
        //move selected piece to location of selected move
        Vector3 position = this.gameObject.transform.localPosition;

        //check if the piece is red or black to add to the game state
        int r_or_b = 0;
        if (this.gameObject.name.Equals("r_piece(Clone)(Clone)"))
        {
            r_or_b = 1;
        }
        else
        {
            r_or_b = 2;
        }
        //new position on the game board from the position of the hit game object
        int new_x = getX(position);
        int new_y = getY(position);
        //edit our game satate
        gameState[new_x, new_y] = r_or_b;

        //key to check if the move was a taking move
        string key = new_x.ToString() + "," + new_y.ToString();
        //if it's a taking move
        if (take_moves_dict.ContainsKey(key))
        {
            foreach (int[] pieces in take_moves_dict[key])
            {
                gameState[pieces[0], pieces[1]] = 0;
                Destroy(objGameState[pieces[0], pieces[1]]);
                objGameState[pieces[0], pieces[1]] = null;

            }
            take_moves_dict = new Dictionary<string, List<int[]>>();//reset take posibilities

        }
        //change the object game state so the old location is empty
        objGameState[selected_x, selected_y] = null;

        //create new object at the new location
        GameObject p = Instantiate(selected, board.transform);
        p.transform.localPosition = position;
        p.name = selected.name;
        //set the object game state to the new piece
        objGameState[new_x, new_y] = p;
        //check for king
        if (kings.Contains(selected))
        {
            //add new king object and get rid of the old one 
            kings.Add(p);
            kings.Remove(selected);
        }
        else
        {
            //if the piece has reached the opposite end (king me!)
            if ((p.name.Equals("r_piece(Clone)") & new_y == 7) | (p.name.Equals("b_piece(Clone)") & new_y == 0))
            {
                kings.Add(p);
                //the height should be doubled to indicate it's a king
                p.transform.localScale += new Vector3(0, 1, 0);
            }
        }
        //get rid of the old object
        Destroy(selected);
        //add a box collider for touch recognition
        p.AddComponent<BoxCollider>();

        //remove all possible moves
        foreach (GameObject obj in pos_moves)
        {
            Destroy(obj);
        }
        pos_moves = new List<GameObject>();

    }
}


